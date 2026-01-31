using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

public class ResourceRegistry<T> where T : UnityEngine.Object
{
    private T[] _assets = Array.Empty<T>();
    private Dictionary<string, int> _idToIndex = new();

    public void Initialize(T[] assets, string[] ids)
    {
        _assets = assets ?? Array.Empty<T>();
        _idToIndex = BuildIdToIndexMap(ids);
    }

    private static Dictionary<string, int> BuildIdToIndexMap(string[] ids)
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

    public T Get(int index) => (uint)index < (uint)_assets.Length ? _assets[index] : null;
    public T GetById(string id) => !string.IsNullOrEmpty(id) && _idToIndex.TryGetValue(id, out int idx) ? _assets[idx] : null;
    public int GetIndexById(string id) => !string.IsNullOrEmpty(id) && _idToIndex.TryGetValue(id, out int idx) ? idx : -1;
    public T[] GetAll() => _assets;
}


public static class GameResDB
{
    public static bool IsInitialized { get; private set; }

    // 跨引用缓存
    static int[] _danmakuConfigToPrefabIndex = null;
    static int[][] _emitterToDanmakuIndices = null;

    // 类型化配置缓存
    static Dictionary<Type, GameConfig[]> _typedConfigsCache = null;

    // 各类资源注册器（内部使用）
    static readonly ResourceRegistry<GameConfig> ConfigRegistry = new();
    static readonly ResourceRegistry<GameObject> PrefabRegistry = new();
    static readonly ResourceRegistry<Texture2D> TextureRegistry = new();
    static readonly ResourceRegistry<SpriteAtlas> AtlasRegistry = new();

    public static async Task InitializeAsync()
    {
        if (IsInitialized) return;

        Logger.Info("Initializing GameResDB...", LogTag.Resource);

        var manifest = ResManager.Instance.Manifest;
        if (manifest == null)
            throw new InvalidOperationException("GameResourceManifest is missing!");

        // 并行加载各类资源（可选：按需串行）
        var loadTasks = new List<Task>();
        foreach (var category in manifest.resourceCategories)
        {
            switch (category.resCategory)
            {
                case E_ResourceCategory.Config:
                    loadTasks.Add(LoadAndRegisterConfigs(category));
                    break;
                case E_ResourceCategory.Prefab:
                    loadTasks.Add(LoadAndRegisterPrefabs(category));
                    break;
                case E_ResourceCategory.Texture:
                    loadTasks.Add(LoadAndRegisterTextures(category));
                    break;
                case E_ResourceCategory.Atlas:
                    loadTasks.Add(LoadAndRegisterAtlas(category));
                    break;
                default:
                    Logger.Warn($"Unsupported resource category: {category.resCategory}");
                    break;
            }
        }

        await Task.WhenAll(loadTasks);

        ResolveCrossReferences();
        BuildTypedConfigCache();

        IsInitialized = true;
        Logger.Info("GameResDB initialized successfully.", LogTag.Resource);
    }

    // —————— 加载与注册 ——————

    static async Task LoadAndRegisterConfigs(ResourceCategory category)
    {
        var ids = CollectAllIds(category);
        var assets = await LoadAssetsAsync<GameConfig>(ids, category.resCategory);
        ConfigRegistry.Initialize(assets, ids);
    }

    static async Task LoadAndRegisterPrefabs(ResourceCategory category)
    {
        var ids = CollectAllIds(category);
        if (ids.Length > 0)
            await ResManager.Instance.PreloadAsync<GameObject>(E_ResourceCategory.Prefab, ids);
        var assets = await LoadAssetsAsync<GameObject>(ids, category.resCategory);
        PrefabRegistry.Initialize(assets, ids);
    }

    static async Task LoadAndRegisterTextures(ResourceCategory category)
    {
        var ids = CollectAllIds(category);
        var assets = await LoadAssetsAsync<Texture2D>(ids, category.resCategory);
        TextureRegistry.Initialize(assets, ids);
    }

    static async Task LoadAndRegisterAtlas(ResourceCategory category)
    {
        var ids = CollectAllIds(category);
        var assets = await LoadAssetsAsync<SpriteAtlas>(ids, category.resCategory);
        AtlasRegistry.Initialize(assets, ids);
    }

    static async Task<T[]> LoadAssetsAsync<T>(IList<string> ids, E_ResourceCategory category) where T : UnityEngine.Object
    {
        if (ids.Count == 0) return Array.Empty<T>();

        var tasks = new Task<T>[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            tasks[i] = ResManager.Instance.LoadAsync<T>(category, ids[i]);
        }

        var results = await Task.WhenAll(tasks);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] == null)
                Logger.Error($"Failed to load {typeof(T).Name}: '{ids[i]}'", LogTag.Resource);
        }
        return results;
    }

    static string[] CollectAllIds(ResourceCategory category)
    {
        var list = new List<string>();
        foreach (var group in category.resGroups)
        {
            if (group?.resourceIds != null)
                list.AddRange(group.resourceIds.Where(id => !string.IsNullOrEmpty(id)));
        }
        return list.ToArray();
    }

    // —————— 引用解析 ——————

    static void ResolveCrossReferences()
    {
        var allConfigs = ConfigRegistry.GetAll();
        _danmakuConfigToPrefabIndex = new int[allConfigs.Length];

        for (int i = 0; i < allConfigs.Length; i++)
        {
            if (allConfigs[i] is DanmakuConfig danmaku)
            {
                _danmakuConfigToPrefabIndex[i] = PrefabRegistry.GetIndexById(danmaku.ConfigId);
            }
            else
            {
                _danmakuConfigToPrefabIndex[i] = -1;
            }
        }

        ParseEmitterReferences();
    }

    static void ParseEmitterReferences()
    {
        var emitters = new List<DanmakuEmitterConfig>();
        var configs = ConfigRegistry.GetAll();
        for (int i = 0; i < configs.Length; i++)
        {
            if (configs[i] is DanmakuEmitterConfig emitter)
                emitters.Add(emitter);
        }

        var danmakuIdToIndex = new Dictionary<string, int>();
        for (int i = 0; i < configs.Length; i++)
        {
            if (configs[i] is DanmakuConfig danmaku)
                danmakuIdToIndex[danmaku.ConfigId] = i;
        }

        _emitterToDanmakuIndices = new int[emitters.Count][];
        for (int i = 0; i < emitters.Count; i++)
        {
            var emitter = emitters[i];
            if (emitter?.danmakuConfigIds == null)
            {
                _emitterToDanmakuIndices[i] = Array.Empty<int>();
                continue;
            }

            var indices = new int[emitter.danmakuConfigIds.Length];
            for (int j = 0; j < indices.Length; j++)
            {
                string id = emitter.danmakuConfigIds[j];
                indices[j] = danmakuIdToIndex.TryGetValue(id, out int idx) ? idx : -1;
                if (indices[j] == -1)
                    Logger.Error($"Emitter '{emitter.ConfigId}' references unknown danmaku config: '{id}'");
            }
            _emitterToDanmakuIndices[i] = indices;
        }
    }

    // —————— 枚举映射构建 ——————

    static void BuildTypedConfigCache()
    {
        var allConfigs = ConfigRegistry.GetAll();
        var typeMap = new Dictionary<Type, List<GameConfig>>();

        foreach (var cfg in allConfigs)
        {
            if (cfg == null) continue;
            var type = cfg.GetType();
            if (!typeMap.TryGetValue(type, out var list))
            {
                list = new List<GameConfig>();
                typeMap[type] = list;
            }
            list.Add(cfg);
        }

        _typedConfigsCache = typeMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
    }

    // —————— 公共访问接口 ——————

    // Config
    public static T GetConfig<T>(string configId) where T : GameConfig
    {
        var cfg = ConfigRegistry.GetById(configId);
        return cfg as T;
    }

    public static T GetConfig<T>(int index) where T : GameConfig
    {
        var cfg = ConfigRegistry.Get(index);
        return cfg as T;
    }

    public static T[] GetAllConfigs<T>() where T : GameConfig
    {
        if (_typedConfigsCache == null)
            throw new InvalidOperationException("GameResDB not initialized.");

        return _typedConfigsCache.TryGetValue(typeof(T), out var array)
            ? Array.ConvertAll(array, c => (T)c)
            : Array.Empty<T>();
    }

    // Prefab
    public static GameObject GetPrefab(string id) => PrefabRegistry.GetById(id);
    public static GameObject GetPrefab(int index) => PrefabRegistry.Get(index);

    // Texture
    public static Texture2D GetTexture(string id) => TextureRegistry.GetById(id);

    // Atlas & Sprite
    public static Sprite GetSpriteFromAtlas(string atlasId, string spriteName)
    {
        var atlas = AtlasRegistry.GetById(atlasId);
        if (atlas == null)
        {
            Logger.Error($"Atlas '{atlasId}' not found.", LogTag.Resource);
            return null;
        }

        var sprite = atlas.GetSprite(spriteName);
        if (sprite == null)
            Logger.Error($"Sprite '{spriteName}' not found in atlas '{atlasId}'.", LogTag.Resource);
        return sprite;
    }

    public static Sprite GetSpriteFromTexture(string textureId, float pixelsPerUnit = 100f)
    {
        var texture = GetTexture(textureId);
        if (texture == null)
        {
            Logger.Error($"Texture '{textureId}' not found.", LogTag.Resource);
            return null;
        }
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    // 跨引用（不变）
    public static int GetDanmakuPrefabIndex(int danmakuConfigIndex) =>
        (uint)danmakuConfigIndex < (uint)_danmakuConfigToPrefabIndex?.Length
            ? _danmakuConfigToPrefabIndex[danmakuConfigIndex]
            : -1;

    public static int[] GetEmitterDanmakuIndices(int emitterIndex) =>
        (uint)emitterIndex < (uint)_emitterToDanmakuIndices?.Length
            ? _emitterToDanmakuIndices[emitterIndex]
            : Array.Empty<int>();
}