using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class ConfigHelper
{
    public const string CONFIG_PREFIX = "Configs/";
    public const string CHARACTER_CONFIG_PREFIX = CONFIG_PREFIX + "Characters/";
    public const string WEAPON_CONFIG_PREFIX = CONFIG_PREFIX + "Weapons/";
    public const string DANMAKU_CONFIG_PREFIX = CONFIG_PREFIX + "Danmakus/";
    public const string DANMAKU_EMITTER_CONFIG_PREFIX = CONFIG_PREFIX + "DanmakuEmitters/";

    public static string[] allCharCfgIds = Enum.GetValues(typeof(E_Character))
                                                .Cast<E_Character>()
                                                .Where(e => e != E_Character.None)
                                                .Select(e => e.ToString())
                                                .ToArray();

    public static string[] allWeapCfgIds = Enum.GetValues(typeof(E_Weapon))
                                                .Cast<E_Weapon>()
                                                .Where(e => e != E_Weapon.None)
                                                .Select(e => e.ToString())
                                                .ToArray();

    public static string BattleAreaCfgId = "DefaultBattleArea";

    /// <summary>
    /// 根据配置类型和ID获取资源key
    /// </summary>
    public static string GetConfigKey<T>(string cfgId) where T : GameConfig
    {
        if (string.IsNullOrEmpty(cfgId))
            throw new ArgumentException("Config ID cannot be null or empty.", nameof(cfgId));

        // 编译期确定，无任何运行时开销
        string prefix = typeof(T) switch
        {
            _ when typeof(T) == typeof(CharacterConfig) => CHARACTER_CONFIG_PREFIX,
            _ when typeof(T) == typeof(WeaponConfig) => WEAPON_CONFIG_PREFIX,

            _ => CONFIG_PREFIX
        };

        return prefix + cfgId;
    }
}

public static class ConfigManager
{
    /// <summary>
    /// 异步获取
    /// </summary>
    public static async Task<T> GetConfigAsync<T>(string cfgId) where T : GameConfig
    {
        if (string.IsNullOrEmpty(cfgId))
            throw new ArgumentException("Config ID cannot be null or empty.", nameof(cfgId));

        string assetPath = ConfigHelper.GetConfigKey<T>(cfgId);

        try
        {
            T config = await ResManager.LoadAsync<T>(assetPath);

            if (config == null)
            {
                Logger.Warn($"Config not found: {assetPath}", LogTag.Config);
                return null;
            }

            return config;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load config {typeof(T).Name} with ID '{cfgId}': {ex.Message}", LogTag.Config);
            return null;
        }
    }

    /// <summary>
    /// 同步获取
    /// </summary>
    public static T GetConfig<T>(string cfgId) where T : GameConfig
    {
        string assetPath = ConfigHelper.GetConfigKey<T>(cfgId);
        return ResManager.Get<T>(assetPath);
    }

    /// <summary>
    /// 同步多项获取
    /// </summary>
    public static T[] GetConfig<T>(string[] cfgIds) where T : GameConfig
    {
        if (cfgIds == null)
            return Array.Empty<T>();

        var results = new T[cfgIds.Length];
        for (int i = 0; i < cfgIds.Length; i++)
        {
            results[i] = GetConfig<T>(cfgIds[i]); // 复用单个获取逻辑
        }
        return results;
    }

    /// <summary>
    /// 异步多项预加载
    /// </summary>
    public static async Task PreloadConfigsAsync<T>(string[] cfgIds) where T : GameConfig
    {
        if (cfgIds == null || cfgIds.Length == 0) return;

        var paths = new string[cfgIds.Length];
        for (int i = 0; i < cfgIds.Length; i++)
        {
            paths[i] = ConfigHelper.GetConfigKey<T>(cfgIds[i]);
        }

        await ResManager.PreloadAsync<T>(paths);
    }
}