using System;
using System.Threading.Tasks;

/// <summary>
/// 对 ResManager 的业务封装，用于异步加载指定游戏资源。
/// 仅允许在游戏启动/关卡加载阶段使用！
/// ECS 系统运行时必须通过 GameResDB 访问配置（零分配、帧同步安全）。
/// </summary>
public static class ResLoader
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
}