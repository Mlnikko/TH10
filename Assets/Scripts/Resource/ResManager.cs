using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

/// <summary>
/// 资源加载管理器（全自动引用计数管理）
/// 仅用于启动加载或预加载！运行时请通过 GameResDB 或组件持有引用访问。
/// 所有加载的资源由 Addressables 自动管理生命周期，无需手动释放。
/// </summary>
public static class ResManager
{
    /// <summary>
    /// 异步加载单个资源
    /// </summary>
    public static async Task<T> LoadAsync<T>(string key) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Resource key cannot be null or empty.", nameof(key));

        var handle = Addressables.LoadAssetAsync<T>(key);
        try
        {
            return await handle.Task;
        }
        catch (Exception ex)
        {
            // 加载失败时释放 handle，避免引用泄漏
            Addressables.Release(handle);
            Logger.Error( $"Failed to load asset '{key}' of type {typeof(T).Name}: {ex.Message}", LogTag.Resource);
            throw; // 保留异常类型，便于上层处理
        }
    }

    /// <summary>
    /// 并行预加载多个资源（提升后续加载速度）
    /// 注意：此方法仅触发加载，不返回结果。资源仍需通过 LoadAsync 或配置引用获取。
    /// </summary>
    public static async Task PreloadAsync<T>(IList<string> keys) where T : UnityEngine.Object
    {
        if (keys == null || keys.Count == 0)
            return;

        var tasks = new List<Task>(keys.Count);
        int validKeyCount = 0;

        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key))
            {
                Logger.Warn("Skipped null or empty resource key during preload.", LogTag.Resource);
                continue;
            }

            validKeyCount++;
            // 启动加载任务，但不 await 单个结果（允许并行）
            tasks.Add(LoadAsync<T>(key).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // 单个资源加载失败不影响整体流程（预加载容错）
                    Logger.Warn( $"Preload failed for key: {key}", LogTag.Resource);
                }
            }));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
            Logger.Info( $"Preloaded {validKeyCount} assets of type {typeof(T).Name}", LogTag.Resource);
        }
    }

    /// <summary>
    /// 重载：支持 params 语法的预加载
    /// </summary>
    public static Task PreloadAsync<T>(params string[] keys) where T : UnityEngine.Object
    {
        return PreloadAsync<T>((IList<string>)keys);
    }
}