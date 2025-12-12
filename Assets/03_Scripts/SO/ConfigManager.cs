using System;
using System.Linq;
using System.Threading.Tasks;

public static class ConfigHelper
{
    public const string CONFIG_ROOT = "Configs/";

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

    /// <summary>
    /// 根据配置类型和ID获取资源key
    /// </summary>
    public static string GetKey<T>(string id) where T : GameConfig
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Config ID cannot be null or empty.", nameof(id));

        return $"{CONFIG_ROOT}{typeof(T).Name}/{id}";
    }
}

public static class ConfigManager
{
    /// <summary>
    /// 异步获取
    /// </summary>
    public static async Task<T> GetConfigAsync<T>(string id) where T : GameConfig
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Config ID cannot be null or empty.", nameof(id));

        string assetPath = ConfigHelper.GetKey<T>(id);

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
            Logger.Error($"Failed to load config {typeof(T).Name} with ID '{id}': {ex.Message}", LogTag.Config);
            return null;
        }
    }

    /// <summary>
    /// 同步获取
    /// </summary>
    public static T GetConfig<T>(string id) where T : GameConfig
    {
        string assetPath = ConfigHelper.GetKey<T>(id);
        return ResManager.Get<T>(assetPath);
    }

    /// <summary>
    /// 同步多项获取
    /// </summary>
    public static T[] GetConfig<T>(string[] ids) where T : GameConfig
    {
        if (ids == null)
            return Array.Empty<T>();

        var results = new T[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            results[i] = GetConfig<T>(ids[i]); // 复用单个获取逻辑
        }
        return results;
    }

    /// <summary>
    /// 异步多项预加载
    /// </summary>
    public static async Task PreloadConfigsAsync<T>(string[] ids) where T : GameConfig
    {
        if (ids == null || ids.Length == 0) return;

        var paths = new string[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            paths[i] = ConfigHelper.GetKey<T>(ids[i]);
        }

        await ResManager.PreloadAsync<T>(paths);
    }
}