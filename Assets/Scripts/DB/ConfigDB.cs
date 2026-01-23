using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 泛型配置注册表 —— 每个 T 有独立的 configs 数组和 idToIndex 映射
/// </summary>
public static class GenericConfigRegistry<T> where T : GameConfig
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

public static class ConfigDB
{
    public static bool IsInitialized { get; private set; }

    public static async Task InitializeAsync()
    {
        if (IsInitialized) return;

        var manifest = await ConfigLoader.GetConfigAsync<GameConfigManifest>(ResHelper.GAME_CONFIG_CHECKLIST);
        if (manifest == null)
            Logger.Exception(new InvalidOperationException("GameConfigManifest not found."));

        // 并行加载并注册
        var tasks = new[]
        {
            RegisterConfigsAsync<DanmakuConfig>(manifest.danmakuConfigIds),
            RegisterConfigsAsync<CharacterConfig>(manifest.characterConfigIds),
            RegisterConfigsAsync<WeaponConfig>(manifest.weaponConfigIds),
            RegisterConfigsAsync<DanmakuEmitterConfig>(manifest.emitterConfigIds)
        };

        await Task.WhenAll(tasks);

        // 特殊处理：Emitter 需要引用 Danmaku 的 IdToIndex
        ParseEmitterReferences(manifest.emitterConfigIds);

        IsInitialized = true;
        Logger.Info("ConfigDB initialized via generic registry.");
    }

    static async Task RegisterConfigsAsync<T>(string[] ids) where T : GameConfig
    {
        if (ids == null || ids.Length == 0)
        {
            GenericConfigRegistry<T>.Initialize(Array.Empty<T>(), Array.Empty<string>());
            return;
        }

        var configs = new T[ids.Length];
        var loadTasks = new Task<T>[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            loadTasks[i] = ConfigLoader.GetConfigAsync<T>(ids[i]);
        }

        var results = await Task.WhenAll(loadTasks);
        for (int i = 0; i < results.Length; i++)
        {
            configs[i] = results[i];
            if (configs[i] == null)
                Logger.Error( $"Failed to load {typeof(T).Name}: {ids[i]}");
        }

        GenericConfigRegistry<T>.Initialize(configs, ids);
    }

    // 特殊处理跨配置引用（如 Emitter → Danmaku）
    static void ParseEmitterReferences(string[] emitterIds)
    {
        var emitters = GenericConfigRegistry<DanmakuEmitterConfig>.Configs;
        var danmakuIdToIndex = GenericConfigRegistry<DanmakuConfig>.IdToIndex;

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
                    Logger.Error( $"Emitter '{emitter.ConfigId}' references unknown danmaku: {id}");
            }
            emitterToDanmakuIndices[i] = indices;
        }

        // 存储到专用字段（因为这是“派生数据”，不属于原始 Config）
        _emitterToDanmakuIndices = emitterToDanmakuIndices;
    }

    // === 保留派生数据字段 ===
    static int[][] _emitterToDanmakuIndices = null;

    // ========================
    // 公共访问 API（保持兼容性）
    // ========================

    // ECS 访问（推荐）
    public static DanmakuConfig GetDanmaku(int index) => GenericConfigRegistry<DanmakuConfig>.Get(index);
    public static CharacterConfig GetCharacter(int index) => GenericConfigRegistry<CharacterConfig>.Get(index);
    public static WeaponConfig GetWeapon(int index) => GenericConfigRegistry<WeaponConfig>.Get(index);
    public static DanmakuEmitterConfig GetEmitter(int index) => GenericConfigRegistry<DanmakuEmitterConfig>.Get(index);

    // UI 访问（推荐）
    public static DanmakuConfig GetDanmakuById(string id) => GenericConfigRegistry<DanmakuConfig>.GetById(id);
    public static CharacterConfig GetCharacterById(string id) => GenericConfigRegistry<CharacterConfig>.GetById(id);
    public static WeaponConfig GetWeaponById(string id) => GenericConfigRegistry<WeaponConfig>.GetById(id);
    public static DanmakuEmitterConfig GetEmitterById(string id) => GenericConfigRegistry<DanmakuEmitterConfig>.GetById(id);

    public static CharacterConfig[] GetCharacters() => GenericConfigRegistry<CharacterConfig>.Configs;

    // 通用泛型访问（高级用法）
    public static T GetConfig<T>(int index) where T : GameConfig => GenericConfigRegistry<T>.Get(index);
    public static T GetConfigById<T>(string id) where T : GameConfig => GenericConfigRegistry<T>.GetById(id);

    // 派生数据
    public static int[] GetEmitterDanmakuIndices(int emitterIndex) =>
        (uint)emitterIndex < (uint)_emitterToDanmakuIndices?.Length
            ? _emitterToDanmakuIndices[emitterIndex]
            : null;
}