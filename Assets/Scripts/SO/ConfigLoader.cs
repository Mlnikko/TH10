using System;
using System.Threading.Tasks;


/// <summary>
/// 对 ResManager 的业务封装，专门用于异步加载游戏配置数据。
/// 仅允许在游戏启动/关卡加载阶段使用！
/// ECS 系统运行时必须通过 ConfigDB 访问配置（零分配、帧同步安全）。
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// 异步加载单个配置
    /// </summary>
    public static async Task<T> GetConfigAsync<T>(string cfgId) where T : GameConfig
    {
        if (string.IsNullOrEmpty(cfgId))
            throw new ArgumentException("Config ID cannot be null or empty.", nameof(cfgId));

        string assetPath = ResHelper.GetConfigKey<T>(cfgId);

        try
        {
            T config = await ResManager.LoadAsync<T>(assetPath);
            if (config == null)
            {
                Logger.Warn( $"Config not found: {assetPath}", LogTag.Config);
                return null;
            }
            return config;
        }
        catch (Exception ex)
        {
            Logger.Error( $"Failed to load config {typeof(T).Name} with ID '{cfgId}': {ex.Message}", LogTag.Config);
            return null;
        }
    }

    /// <summary>
    /// 异步加载多个配置（并行）
    /// </summary>
    public static async Task<T[]> GetConfigsAsync<T>(string[] cfgIds) where T : GameConfig
    {
        if (cfgIds == null || cfgIds.Length == 0)
            return Array.Empty<T>();

        var tasks = new Task<T>[cfgIds.Length];
        for (int i = 0; i < cfgIds.Length; i++)
        {
            if (string.IsNullOrEmpty(cfgIds[i]))
            {
                Logger.Warn( $"Skipped null/empty config ID at index {i}", LogTag.Config);
                tasks[i] = Task.FromResult<T>(null);
            }
            else
            {
                tasks[i] = GetConfigAsync<T>(cfgIds[i]);
            }
        }

        T[] results = await Task.WhenAll(tasks);

        // 可选：过滤 null（根据需求决定）
        // return results.Where(r => r != null).ToArray();

        return results;
    }

    /// <summary>
    /// 预加载多个配置（仅触发加载，不返回结果，用于加速后续 GetConfigAsync）
    /// </summary>
    public static async Task PreloadConfigsAsync<T>(string[] cfgIds) where T : GameConfig
    {
        if (cfgIds == null || cfgIds.Length == 0)
            return;

        var paths = new string[cfgIds.Length];
        int validCount = 0;

        for (int i = 0; i < cfgIds.Length; i++)
        {
            if (!string.IsNullOrEmpty(cfgIds[i]))
            {
                paths[validCount++] = ResHelper.GetConfigKey<T>(cfgIds[i]);
            }
            else
            {
                Logger.Warn("Skipped null or empty config ID in preload.", LogTag.Config);
            }
        }

        if (validCount > 0)
        {
            // 截取有效部分（避免传递 null 路径）
            Array.Resize(ref paths, validCount);
            await ResManager.PreloadAsync<T>(paths);
        }
    }
}