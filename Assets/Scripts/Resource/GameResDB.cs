using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#region 注册表
/// <summary>
/// 泛型配置注册表 —— 每个 T 有独立的 configs 数组和 idToIndex 映射
/// </summary>
public static class ConfigRegistry<T> where T : GameConfig
{
    public static T[] Configs { get; private set; } = Array.Empty<T>();
    public static Dictionary<string, int> IdToIndex { get; private set; } = new();

    internal static void Initialize(T[] configs, string[] ids)
    {
        Configs = configs ?? Array.Empty<T>();
        IdToIndex = BuildIdToIndexMap(ids);
    }

    static Dictionary<string, int> BuildIdToIndexMap(string[] ids)
    {
        if (ids == null) return new Dictionary<string, int>();
        var map = new Dictionary<string, int>(ids.Length);
        for (int i = 0; i < ids.Length; i++)
        {
            if (!string.IsNullOrEmpty(ids[i]))
                map[ids[i]] = i;
        }
        return map;
    }

    // 运行时访问（ECS）
    public static T Get(int index) =>
        (uint)index < (uint)Configs.Length ? Configs[index] : null;

    // 运行时访问（UI）
    public static T GetById(string id) =>
        !string.IsNullOrEmpty(id) && IdToIndex.TryGetValue(id, out int idx)
            ? Configs[idx]
            : null;
}

/// <summary>
/// 统一的 Prefab 注册中心 —— 按 PrefabCategory 分类存储
/// </summary>
public static class PrefabRegistry
{
    static readonly Dictionary<PrefabCategory, GameObject[]> _prefabs = new();
    static readonly Dictionary<PrefabCategory, Dictionary<string, int>> _idToIndex = new();

    // 初始化时由 GameDB 调用
    internal static void Initialize(PrefabCategory category, GameObject[] prefabs, string[] ids)
    {
        prefabs ??= Array.Empty<GameObject>();
        _prefabs[category] = prefabs;

        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        if (ids != null)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]))
                    map[ids[i]] = i;
            }
        }
        _idToIndex[category] = map;
    }

    // === 运行时访问 API ===

    public static GameObject Get(PrefabCategory category, int index)
    {
        return (uint)index < (uint)_prefabs.GetValueOrDefault(category, Array.Empty<GameObject>()).Length
            ? _prefabs[category][index]
            : null;
    }

    public static GameObject GetById(PrefabCategory category, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!_idToIndex.TryGetValue(category, out var map)) return null;
        return map.TryGetValue(id, out int idx) ? Get(category, idx) : null;
    }

    public static int GetIndexById(PrefabCategory category, string id)
    {
        if (string.IsNullOrEmpty(id)) return -1;
        return _idToIndex.TryGetValue(category, out var map) && map.TryGetValue(id, out int idx)
            ? idx
            : -1;
    }

    // 可选：获取全部（用于调试/UI）
    public static GameObject[] GetAll(PrefabCategory category) => _prefabs.GetValueOrDefault(category, Array.Empty<GameObject>());
}
#endregion

public static class GameResDB
{
    public static bool IsInitialized { get; private set; }

    public static async Task InitializeAsync()
    {
        if (IsInitialized) return;

        Logger.Info("Initializing GameResDB...", LogTag.Resource);

        var configManifest = await ResLoader.GetConfigAsync<GameConfigManifest>(ResHelper.GAME_CONFIG_CHECKLIST);
        if (configManifest == null) Logger.Critical("GameConfigManifest not found.");

        var prefabManifest = await ResLoader.GetConfigAsync<GamePrefabManifest>(ResHelper.GAME_PREFAB_CHECKLIST);
        if (prefabManifest == null) Logger.Critical("GamePrefabManifest not found.");

        // 2. 加载注册配置表
        await BuildConfigRegistries(configManifest);

        // 3. 构建 Prefab 注册表（含反向映射）
        await BuildPrefabRegistries(prefabManifest);

        // 4. 解析跨配置引用
        ResolveCrossReferences(configManifest, prefabManifest);

        IsInitialized = true;
        Logger.Info("GameDB initialized successfully.", LogTag.Resource);
    }

    static async Task BuildConfigRegistries(GameConfigManifest manifest)
    {
        var tasks = new[]
        {
            RegisterConfigsAsync<DanmakuConfig>(manifest.danmakuConfigIds),
            RegisterConfigsAsync<CharacterConfig>(manifest.characterConfigIds),
            RegisterConfigsAsync<WeaponConfig>(manifest.weaponConfigIds),
            RegisterConfigsAsync<DanmakuEmitterConfig>(manifest.emitterConfigIds)
        };
        await Task.WhenAll(tasks);
    }
    static async Task BuildPrefabRegistries(GamePrefabManifest manifest)
    {
        var tasks = new[]
        {
            RegisterPrefabsAsync(manifest.danmakuPrefabIds, PrefabCategory.Danmaku),
            RegisterPrefabsAsync(manifest.characterPrefabIds, PrefabCategory.Character),
            //
        };
        await Task.WhenAll(tasks);
    }

    static async Task RegisterConfigsAsync<T>(string[] ids) where T : GameConfig
    {
        if (ids == null || ids.Length == 0)
        {
            ConfigRegistry<T>.Initialize(Array.Empty<T>(), Array.Empty<string>());
            return;
        }

        var configs = new T[ids.Length];
        var loadTasks = new Task<T>[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            loadTasks[i] = ResLoader.GetConfigAsync<T>(ids[i]);
        }

        var results = await Task.WhenAll(loadTasks);
        for (int i = 0; i < results.Length; i++)
        {
            configs[i] = results[i];
            if (configs[i] == null)
                Logger.Error( $"Failed to load {typeof(T).Name}: {ids[i]}");
        }

        ConfigRegistry<T>.Initialize(configs, ids);
    }
    static async Task RegisterPrefabsAsync(string[] ids, PrefabCategory category)
    {
        if (ids == null || ids.Length == 0)
        {
            PrefabRegistry.Initialize(category, Array.Empty<GameObject>(), Array.Empty<string>());
            return;
        }

        var keys = new string[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            keys[i] = ResHelper.GetPrefabKey(ids[i], category);
        }

        await ResManager.PreloadAsync<GameObject>(keys);

        var loadTasks = new Task<GameObject>[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            loadTasks[i] = ResManager.LoadAsync<GameObject>(keys[i]);
        }

        var prefabs = await Task.WhenAll(loadTasks);
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == null)
                Logger.Error($"Failed to load prefab: {keys[i]}", LogTag.Resource);
        }

        PrefabRegistry.Initialize(category, prefabs, ids);
    }

    static void ResolveCrossReferences(GameConfigManifest configManifest, GamePrefabManifest prefabManifest)
    {
        // 1. DanmakuConfig.prefabId → prefabIndex
        var danmakuConfigs = ConfigRegistry<DanmakuConfig>.Configs;
        _danmakuConfigToPrefabIndex = new int[danmakuConfigs.Length];
        for (int i = 0; i < danmakuConfigs.Length; i++)
        {
            var cfg = danmakuConfigs[i];
            if (cfg != null && !string.IsNullOrEmpty(cfg.prefabId))
            {
                int idx = PrefabRegistry.GetIndexById(PrefabCategory.Danmaku, cfg.prefabId);
                _danmakuConfigToPrefabIndex[i] = idx; // -1 if not found
            }
            else
            {
                _danmakuConfigToPrefabIndex[i] = -1;
            }
        }

        // 2. Emitter → Danmaku indices
        ParseEmitterReferences(configManifest.emitterConfigIds);
    }

    // 特殊处理跨配置引用
    static void ParseEmitterReferences(string[] emitterIds)
    {
        var emitters = ConfigRegistry<DanmakuEmitterConfig>.Configs;
        var danmakuIdToIndex = ConfigRegistry<DanmakuConfig>.IdToIndex;

        var emitterToDanmakuIndices = new int[emitters.Length][];
        for (int i = 0; i < emitters.Length; i++)
        {
            var emitter = emitters[i];
            if (emitter?.danmakuConfigIds == null)
            {
                emitterToDanmakuIndices[i] = Array.Empty<int>();
                continue;
            }

            var indices = new int[emitter.danmakuConfigIds.Length];
            for (int j = 0; j < indices.Length; j++)
            {
                string id = emitter.danmakuConfigIds[j];
                indices[j] = danmakuIdToIndex.TryGetValue(id, out int idx) ? idx : -1;
                if (indices[j] == -1)
                    Logger.Error($"Emitter '{emitter.ConfigId}' references unknown danmaku: {id}");
            }
            emitterToDanmakuIndices[i] = indices;
        }

        // 存储到专用字段（因为这是“派生数据”，不属于原始 Config）
        _emitterToDanmakuIndices = emitterToDanmakuIndices;
    }

    // === 保留派生数据字段 ===
    static int[] _danmakuConfigToPrefabIndex = null;
    static int[][] _emitterToDanmakuIndices = null;

    #region 外部访问 API

    // 索引访问（ECS 推荐）
    public static T GetConfig<T>(int index) where T : GameConfig => ConfigRegistry<T>.Get(index);

    public static T[] GetAllConfigs<T>() where T : GameConfig => ConfigRegistry<T>.Configs;

    // ID 访问（UI 推荐）
    public static T GetConfigById<T>(string id) where T : GameConfig => ConfigRegistry<T>.GetById(id);

    public static int GetIndexById<T>(string id) where T : GameConfig
    {
        var map = ConfigRegistry<T>.IdToIndex;
        return !string.IsNullOrEmpty(id) && map.TryGetValue(id, out int idx) ? idx : -1;
    }


    // --- Prefab 访问 ---
    public static GameObject GetPrefabById(PrefabCategory category, string id) => PrefabRegistry.GetById(category, id);
    
    public static GameObject GetPrefab(PrefabCategory category, int index) => PrefabRegistry.Get(category, index);

    // --- 跨引用访问（关键！）---
    public static int GetDanmakuPrefabIndex(int danmakuConfigIndex)
    {
        if ((uint)danmakuConfigIndex < (uint)_danmakuConfigToPrefabIndex.Length)
            return _danmakuConfigToPrefabIndex[danmakuConfigIndex];
        return -1;
    }
    public static int[] GetEmitterDanmakuIndices(int emitterIndex)
    {
        if ((uint)emitterIndex < (uint)_emitterToDanmakuIndices?.Length)
            return _emitterToDanmakuIndices[emitterIndex];
        return null;
    }
    #endregion
}