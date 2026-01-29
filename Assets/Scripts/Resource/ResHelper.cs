using System;

public enum SpriteCategory
{
    Common,

    UI,
    Character,
    Weapon,
    Danmaku,
    Effect
}

public enum PrefabCategory
{
    Common,

    Character,
    Enemy,
    Danmaku,
 
    UI_Panel,
    UI_Item,

    VFX,
}

public enum ConfigCategory
{
    General,
    Character,
    Weapon,
    Danmaku,
    DanmakuEmitter
}

public static class ResHelper
{
    // =============== 配置清单资源 ===============
    public const string GAME_CONFIG_CHECKLIST = "GameConfigManifest";
    public const string GAME_PREFAB_CHECKLIST = "GamePrefabManifest";

    // =============== 配置资源key前缀 ===============
    public const string CONFIG_PREFIX = "Configs/";
    public const string CHARACTER_CONFIG_PREFIX = CONFIG_PREFIX + "Characters/";
    public const string WEAPON_CONFIG_PREFIX = CONFIG_PREFIX + "Weapons/";
    public const string DANMAKU_CONFIG_PREFIX = CONFIG_PREFIX + "Danmakus/";
    public const string DANMAKU_EMITTER_CONFIG_PREFIX = CONFIG_PREFIX + "DanmakuEmitters/";

    // =============== Texture资源key前缀 ===============
    public const string TEXTURE_PREFIX = "Art/Textures/";
    public const string TEXTURE_CHAR_PREFIX = TEXTURE_PREFIX + "Characters/";
    public const string TEXTURE_COMMON_PREFIX = TEXTURE_PREFIX + "Common/";

    // =============== Atlas资源key前缀 ===============
    public const string ATLAS_PREFIX = "Art/Atlases/";

    // =============== 预制体资源key前缀 ===============
    public const string PREFAB_PREFIX = "Prefabs/";
    public const string DANMAKU_PREFAB_PREFIX = PREFAB_PREFIX + "Danmakus/";
    public const string CHARACTER_PREFAB_PREFIX = PREFAB_PREFIX + "Characters/";
    public const string ENEMY_PREFAB_PREFIX = PREFAB_PREFIX + "Enemies/";
    public const string UI_PANEL_PREFIX = PREFAB_PREFIX + "UI/Panels/";
    public const string UI_ITEM_PREFIX = PREFAB_PREFIX + "UI/Items/";
    public const string VFX_PREFIX = PREFAB_PREFIX + "VFX/";


    /// <summary>
    /// 获取 Config 资源的 Addressables Key（仅限 GameConfig 子类）
    /// </summary>
    public static string GetConfigKey<T>(string id) where T : GameConfig
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("ID cannot be null or empty.", nameof(id));

        string prefix = typeof(T) switch
        {
            _ when typeof(T) == typeof(CharacterConfig) => CHARACTER_CONFIG_PREFIX,
            _ when typeof(T) == typeof(WeaponConfig) => WEAPON_CONFIG_PREFIX,
            _ when typeof(T) == typeof(DanmakuConfig) => DANMAKU_CONFIG_PREFIX,
            _ when typeof(T) == typeof(DanmakuEmitterConfig) => DANMAKU_EMITTER_CONFIG_PREFIX,
            _ => CONFIG_PREFIX
        };

        return prefix + id;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="prefabId"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetPrefabKey(string prefabId, PrefabCategory category)
    {
        if (string.IsNullOrEmpty(prefabId))
            throw new ArgumentException("Prefab name cannot be null or empty.", nameof(prefabId));

        string prefix = category switch
        {       
            PrefabCategory.Danmaku => DANMAKU_PREFAB_PREFIX,
            PrefabCategory.Character => CHARACTER_PREFAB_PREFIX,
            PrefabCategory.UI_Panel => UI_PANEL_PREFIX,
            PrefabCategory.UI_Item => UI_ITEM_PREFIX,
            PrefabCategory.VFX => VFX_PREFIX,
            _ => PREFAB_PREFIX
        };

        return prefix + prefabId;
    }

    /// <summary>
    /// 获取单张纹理的资源key
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetTextureKey(string fileName, SpriteCategory category)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException(nameof(fileName));

        string prefix = category switch
        {
            SpriteCategory.Character => TEXTURE_CHAR_PREFIX,
            _ => TEXTURE_COMMON_PREFIX
        };

        return prefix + fileName;
    }

    /// <summary>
    /// 获取指定名称的图集资源key
    /// </summary>
    /// <param name="atlasName">图集名称</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetSpriteAtlasKey(string atlasName)
    {
        if (string.IsNullOrEmpty(atlasName))
            throw new ArgumentException(nameof(atlasName));

        return ATLAS_PREFIX + atlasName; // 如：Art/Atlases/Weapon
    }
}