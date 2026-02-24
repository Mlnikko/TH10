using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

public interface IReferenceResolver
{
    /// <summary>
    /// 在所有资源注册完成后，解析内部字符串 ID 到全局索引
    /// </summary>
    /// <param name="resDb">提供查询其他资源索引的能力</param>
    void ResolveReferences(GameResDB resDb);
}


// —————— 内部工具：资源注册器（不对外暴露） ——————
internal class ResourceRegistry<T> where T : UnityEngine.Object
{
    T[] _assets = Array.Empty<T>();
    Dictionary<string, int> _idToIndex = new();

    public void Initialize(IReadOnlyList<T> assets, IReadOnlyList<string> ids)
    {
        if (assets == null || ids == null || assets.Count != ids.Count)
            throw new ArgumentException("Assets and IDs must be non-null and same length.");

        _assets = assets.ToArray();

        _idToIndex = new Dictionary<string, int>(ids.Count);

        // 从 0 开始编制资源索引
        for (int i = 0; i < ids.Count; i++)
        {
            if (string.IsNullOrEmpty(ids[i]))
                continue;
            if (_idToIndex.ContainsKey(ids[i]))
                Logger.Error($"Duplicate resource ID: {ids[i]}", LogTag.Resource);
            else
                _idToIndex[ids[i]] = i;
        }
    }

    // 仅内部使用（用于初始化 ConfigIndex）
    internal T GetByIndex(int index) =>
        (uint)index < (uint)_assets.Length ? _assets[index] : null;

    internal int GetIndexById(string id) =>
        !string.IsNullOrEmpty(id) && _idToIndex.TryGetValue(id, out int idx) ? idx : -1;

    internal T GetById(string id)
    {
        int index = GetIndexById(id);
        return index >= 0 ? _assets[index] : null;
    }

    internal List<T> GetAssets()
    {
        return new List<T>(_assets);
    }

    internal int Count => _assets.Length;
}

/// <summary>
/// 运行时游戏资源数据库（只通过索引访问）
/// </summary>
public class GameResDB : Singleton<GameResDB>
{
    public static bool IsInitialized { get; private set; }

    // —————— 内部注册器（不对外暴露） ——————
    readonly ResourceRegistry<GameConfig> _configRegistry = new();
    readonly ResourceRegistry<GameObject> _prefabRegistry = new();
    readonly ResourceRegistry<Texture2D> _textureRegistry = new();
    readonly ResourceRegistry<SpriteAtlas> _atlasRegistry = new();

    public int GetPrefabIndex(string id) => _prefabRegistry.GetIndexById(id);
    public int GetConfigIndex(string id) => _configRegistry.GetIndexById(id);
    public int GetTextureIndex(string id) => _textureRegistry.GetIndexById(id);
    public int GetAtlasIndex(string id) => _atlasRegistry.GetIndexById(id);


    public int GetMaxPrefabIndex() => _prefabRegistry.Count;

    // —————— 初始化 ——————
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        Logger.Info("Initializing GameResDB...", LogTag.Resource);

        var manifest = ResManager.Instance.Manifest;
        if (manifest == null)
            throw new InvalidOperationException("GameResourceManifest is null!");

        // —————— 加载 Configs ——————
        {
            var allConfigIds = new List<string>();
            allConfigIds.AddRange(manifest.characterConfigIds);
            allConfigIds.AddRange(manifest.enemyConfigIds);
            allConfigIds.AddRange(manifest.weaponConfigIds);
            allConfigIds.AddRange(manifest.danmakuConfigIds);
            allConfigIds.AddRange(manifest.danmakuEmitterConfigIds);

            if (!string.IsNullOrEmpty(manifest.battleAreaConfigId))
                allConfigIds.Add(manifest.battleAreaConfigId);

            var configAssets = await LoadAssetsAsync<GameConfig>(allConfigIds, E_ResourceCategory.Config);
            _configRegistry.Initialize(configAssets, allConfigIds);

            // 赋值 configIndex（关键！）
            for (int i = 0; i < configAssets.Count; i++)
            {
                configAssets[i].configIndex = i;
            }
        }

        // —————— 加载 Prefabs ——————
        {
            var allPrefabIds = new List<string>();
            allPrefabIds.AddRange(manifest.characterPrefabIds);
            allPrefabIds.AddRange(manifest.enemyPrefabIds);
            allPrefabIds.AddRange(manifest.danmakuPrefabIds);
            allPrefabIds.AddRange(manifest.danmakuEmitterPrefabIds);
            allPrefabIds.AddRange(manifest.effectPrefabIds);

            // 预加载（可选）
            await ResManager.Instance.PreloadAsync<GameObject>(E_ResourceCategory.Prefab, allPrefabIds);
            var prefabAssets = await LoadAssetsAsync<GameObject>(allPrefabIds, E_ResourceCategory.Prefab);
            _prefabRegistry.Initialize(prefabAssets, allPrefabIds);
        }

        // —————— 加载 Textures ——————
        {
            var textureIds = new List<string>(manifest.characterImages);
            var textureAssets = await LoadAssetsAsync<Texture2D>(textureIds, E_ResourceCategory.Texture);
            _textureRegistry.Initialize(textureAssets, textureIds);
        }

        // —————— 加载 Atlases ——————
        {
            var atlasIds = new List<string>(manifest.atlases);
            var atlasAssets = await LoadAssetsAsync<SpriteAtlas>(atlasIds, E_ResourceCategory.Atlas);
            _atlasRegistry.Initialize(atlasAssets, atlasIds);
        }

        ResolveReferences();

        IsInitialized = true;
        Logger.Info("GameResDB initialized successfully.", LogTag.Resource);
    }

    async Task<List<T>> LoadAssetsAsync<T>(IReadOnlyList<string> ids, E_ResourceCategory category) where T : UnityEngine.Object
    {
        if (ids.Count == 0) return new List<T>();

        var tasks = new Task<T>[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            tasks[i] = ResManager.Instance.LoadAsync<T>(category, ids[i]);
        }

        var results = await Task.WhenAll(tasks);
        var loaded = new List<T>(results.Length);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] == null)
                Logger.Error($"Failed to load {typeof(T).Name}: '{ids[i]}'", LogTag.Resource);
            loaded.Add(results[i]);
        }
        return loaded;
    }

    /// <summary>
    /// 配置索引编制，把配置中的string类型ID转换为运行时int索引
    /// </summary>
    void ResolveReferences()
    {
        int configCount = _configRegistry.Count;
        for (int i = 0; i < configCount; i++)
        {
            var cfg = _configRegistry.GetByIndex(i);
            if (cfg is IReferenceResolver resolver)
            {
                resolver.ResolveReferences(this);
            }
        }
    }


    #region Config
    // 通过编制索引获取 Config
    public T GetConfig<T>(int index) where T : GameConfig
    {
        var cfg = _configRegistry.GetByIndex(index);
        return cfg as T;
    }

    // 通过 configId 获取 Config（辅助方法）
    public T GetConfig<T>(string configId) where T : GameConfig
    {
        int index = _configRegistry.GetIndexById(configId);
        return GetConfig<T>(index);
    }

    public List<T> GetConfigs<T>() where T : GameConfig
    {
        // 直接调用 Registry 的方法，拿到所有资产列表
        var allAssets = _configRegistry.GetAssets();

        // 过滤出特定类型 T
        // 注意：这里会有轻微的 LINQ 开销，但只在 Loading 阶段执行一次，完全可接受
        return allAssets.OfType<T>().ToList();
    }
    #endregion

    #region Prefab Access
    public GameObject GetPrefab(int index) => _prefabRegistry.GetByIndex(index);
    #endregion

    #region Texture Access
    public Texture2D GetTexture(int index) => _textureRegistry.GetByIndex(index);
    #endregion

    #region Atlas & Sprite
    public Sprite GetSpriteFromAtlas(int atlasIndex, string spriteName)
    {
        var atlas = _atlasRegistry.GetByIndex(atlasIndex);
        if (atlas == null)
        {
            Logger.Error($"Atlas at index {atlasIndex} not found.", LogTag.Resource);
            return null;
        }
        return atlas.GetSprite(spriteName);
    }

    public Sprite GetSpriteFromAtlas(string atlasId, string spriteName)
    {
        var atlas = _atlasRegistry.GetById(atlasId);
        if (atlas == null)
        {
            Logger.Error($"Atlas with ID '{atlasId}' not found.", LogTag.Resource);
            return null;
        }
        return atlas.GetSprite(spriteName);
    }

    public Sprite GetSpriteFromTexture(int textureIndex, float pixelsPerUnit = 100f)
    {
        var texture = _textureRegistry.GetByIndex(textureIndex);
        if (texture == null)
        {
            Logger.Error($"Texture at index {textureIndex} not found.", LogTag.Resource);
            return null;
        }
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    public Sprite GetSpriteFromTexture(string textureId, float pixelsPerUnit = 100f)
    {
        var texture = _textureRegistry.GetById(textureId);
        if (texture == null)
        {
            Logger.Error($"Texture with ID '{textureId}' not found.", LogTag.Resource);
            return null;
        }
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
    #endregion
}