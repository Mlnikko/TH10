using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

 /// <summary>
/// 在所有资源注册完成后，解析内部字符串 ID 到全局索引
/// </summary>
public interface IReferenceResolver
{
    void ResolveReferences(GameResDB resDb);
}

/// <summary>
/// 将「秒」等时间语义字段烘焙为逻辑帧。
/// </summary>
public interface ILogicTimingBake
{
    void BakeLogicTiming(uint logicFPS);
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

        // 从 0 开始编制资源索引（键均为 NormalizeResourceId 结果）
        for (int i = 0; i < ids.Count; i++)
        {
            string key = StringHelper.NormalizeResourceId(ids[i]);
            if (string.IsNullOrEmpty(key))
                continue;
            if (_idToIndex.ContainsKey(key))
                Logger.Error($"Duplicate resource ID: {key}", LogTag.Resource);
            else
                _idToIndex[key] = i;
        }
    }

    // 仅内部使用（用于初始化 ConfigIndex）
    internal T GetByIndex(int index) =>
        (uint)index < (uint)_assets.Length ? _assets[index] : null;

    internal int GetIndexById(string id)
    {
        string key = StringHelper.NormalizeResourceId(id);
        return !string.IsNullOrEmpty(key) && _idToIndex.TryGetValue(key, out int idx) ? idx : -1;
    }

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
    readonly ResourceRegistry<Sprite> _manifestSpriteRegistry = new();
    readonly ResourceRegistry<SpriteAtlas> _atlasRegistry = new();

    Shader _battleBackgroundScrollShader;

    public const string BattleBackgroundScrollShaderId = "th10_battlebackgroundperspective";

    public Shader BattleBackgroundScrollShader => _battleBackgroundScrollShader;

    public int GetPrefabIndex(string id) => _prefabRegistry.GetIndexById(id);
    public int GetConfigIndex(string id) => _configRegistry.GetIndexById(id);
    public int GetTextureIndex(string id) => _manifestSpriteRegistry.GetIndexById(id);
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
            AppendNormalizedIds(allConfigIds, manifest.characterConfigIds);
            AppendNormalizedIds(allConfigIds, manifest.enemyConfigIds);
            AppendNormalizedIds(allConfigIds, manifest.weaponConfigIds);
            AppendNormalizedIds(allConfigIds, manifest.danmakuConfigIds);
            AppendNormalizedIds(allConfigIds, manifest.danmakuEmitterConfigIds);
            AppendNormalizedIds(allConfigIds, manifest.dropItemConfigIds);
            AppendNormalizedIds(allConfigIds, manifest.poolConfigIds);

            string battleAreaId = StringHelper.NormalizeResourceId(manifest.battleAreaConfigId);
            if (!string.IsNullOrEmpty(battleAreaId))
                allConfigIds.Add(battleAreaId);

            string collisionMatrixId = StringHelper.NormalizeResourceId(manifest.collisionLayerMatrixConfigId);
            if (!string.IsNullOrEmpty(collisionMatrixId))
                allConfigIds.Add(collisionMatrixId);

            AppendNormalizedIds(allConfigIds, manifest.stageTimelineConfigIds);

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
            AppendNormalizedIds(allPrefabIds, manifest.characterPrefabIds);
            AppendNormalizedIds(allPrefabIds, manifest.weaponPrefabIds);
            AppendNormalizedIds(allPrefabIds, manifest.enemyPrefabIds);
            AppendNormalizedIds(allPrefabIds, manifest.danmakuPrefabIds);
            AppendNormalizedIds(allPrefabIds, manifest.danmakuEmitterPrefabIds);
            AppendNormalizedIds(allPrefabIds, manifest.effectPrefabIds);
            AppendPrefabIdsDistinct(allPrefabIds, manifest.dropItemPrefabIds);
            AppendPrefabIdsDistinct(allPrefabIds, manifest.stagePrefabIds);

            // 预加载（可选）
            await ResManager.Instance.PreloadAsync<GameObject>(E_ResourceCategory.Prefab, allPrefabIds);
            var prefabAssets = await LoadAssetsAsync<GameObject>(allPrefabIds, E_ResourceCategory.Prefab);
            _prefabRegistry.Initialize(prefabAssets, allPrefabIds);
        }

        // —————— 加载 Manifest 独立贴图（角色立绘、关卡背景等；导入类型多为 Sprite） ——————
        {
            var spriteIds = new List<string>();
            AppendNormalizedIds(spriteIds, manifest.characterImages);
            AppendNormalizedIds(spriteIds, manifest.stageBackgroundTextureIds);
            var spriteAssets = await LoadManifestSpritesAsync(spriteIds);
            _manifestSpriteRegistry.Initialize(spriteAssets, spriteIds);
        }

        await LoadBattlePresentationShaderAsync();

        // —————— 加载 Atlases ——————
        {
            var atlasIds = new List<string>();
            AppendNormalizedIds(atlasIds, manifest.atlases);
            var atlasAssets = await LoadAssetsAsync<SpriteAtlas>(atlasIds, E_ResourceCategory.Atlas);
            _atlasRegistry.Initialize(atlasAssets, atlasIds);
        }

        InitConfig();

        IsInitialized = true;
        Logger.Info("GameResDB initialized successfully.", LogTag.Resource);
    }

    static void AppendNormalizedIds(List<string> dest, string[] items)
    {
        if (items == null || items.Length == 0)
            return;
        for (int i = 0; i < items.Length; i++)
        {
            string id = StringHelper.NormalizeResourceId(items[i]);
            if (!string.IsNullOrEmpty(id))
                dest.Add(id);
        }
    }

    static void AppendPrefabIdsDistinct(List<string> list, string[] ids)
    {
        if (ids == null || ids.Length == 0)
            return;
        for (int i = 0; i < ids.Length; i++)
        {
            string id = StringHelper.NormalizeResourceId(ids[i]);
            if (string.IsNullOrEmpty(id))
                continue;
            if (!list.Contains(id))
                list.Add(id);
        }
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

    async Task<List<Sprite>> LoadManifestSpritesAsync(IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
            return new List<Sprite>();

        var loaded = new List<Sprite>(ids.Count);
        for (int i = 0; i < ids.Count; i++)
        {
            loaded.Add(await LoadManifestSpriteAsync(ids[i]));
        }

        return loaded;
    }

    static async Task<Sprite> LoadManifestSpriteAsync(string id)
    {
        string key = ResHelper.GetAddressableKey(E_ResourceCategory.Texture, id);

        try
        {
            var sprite = await ResLoader.LoadAsync<Sprite>(key);
            if (sprite != null)
                return sprite;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Load Sprite failed for '{id}', fallback to Texture2D: {ex.Message}", LogTag.Resource);
        }

        try
        {
            var texture = await ResLoader.LoadAsync<Texture2D>(key);
            if (texture != null)
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load manifest sprite '{id}': {ex.Message}", LogTag.Resource);
        }

        Logger.Error($"Failed to load manifest sprite: '{id}'", LogTag.Resource);
        return null;
    }

    async Task LoadBattlePresentationShaderAsync()
    {
        try
        {
            _battleBackgroundScrollShader = await ResManager.Instance.LoadAsync<Shader>(
                E_ResourceCategory.Shader,
                BattleBackgroundScrollShaderId);
        }
        catch (Exception ex)
        {
            Logger.Warn(
                $"Load battle background shader '{BattleBackgroundScrollShaderId}' failed: {ex.Message}",
                LogTag.Resource);
        }

        if (_battleBackgroundScrollShader == null)
        {
            _battleBackgroundScrollShader = Shader.Find("TH10/BattleBackgroundScroll");
            if (_battleBackgroundScrollShader == null)
            {
                Logger.Error(
                    "[GameResDB] Battle background shader missing. Register Addressable shader_th10_battlebackgroundperspective.",
                    LogTag.Resource);
            }
        }
    }

    /// <summary>
    /// 1. 配置索引编制，把配置中的string类型ID转换为运行时int索引
    /// 2. 秒->帧转换
    /// </summary>
    void InitConfig()
    {
        int configCount = _configRegistry.Count;
        for (int i = 0; i < configCount; i++)
        {
            var cfg = _configRegistry.GetByIndex(i);
            if (cfg is IReferenceResolver resolver)
                resolver.ResolveReferences(this);
            if (cfg is ILogicTimingBake timingBake)
                timingBake.BakeLogicTiming(GameManager.logicFPS);
        }

        ApplyCollisionLayerMatrix();
    }

    void ApplyCollisionLayerMatrix()
    {
        var manifest = ResManager.Instance?.Manifest;
        CollisionLayerMatrixConfig matrixCfg = null;
        if (manifest != null && !string.IsNullOrEmpty(manifest.collisionLayerMatrixConfigId))
            matrixCfg = GetConfig<CollisionLayerMatrixConfig>(manifest.collisionLayerMatrixConfigId);

        if (matrixCfg != null)
            ColliderLayerMatrix.Apply(matrixCfg);
        else
        {
            Logger.Warn(
                "[GameResDB] CollisionLayerMatrixConfig not found; using built-in defaults.",
                LogTag.Collision);
            ColliderLayerMatrix.ApplyBuiltInDefaults();
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
    public Texture2D GetTexture(int index)
    {
        var sprite = _manifestSpriteRegistry.GetByIndex(index);
        return sprite != null ? sprite.texture : null;
    }
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
        var sprite = _manifestSpriteRegistry.GetByIndex(textureIndex);
        if (sprite == null)
        {
            Logger.Error($"Manifest sprite at index {textureIndex} not found.", LogTag.Resource);
            return null;
        }

        return sprite;
    }

    public Sprite GetSpriteFromTexture(string textureId, float pixelsPerUnit = 100f)
    {
        var sprite = _manifestSpriteRegistry.GetById(textureId);
        if (sprite == null)
        {
            Logger.Error($"Manifest sprite '{textureId}' not found.", LogTag.Resource);
            return null;
        }

        return sprite;
    }
    #endregion
}