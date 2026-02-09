using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

public enum E_ResourceCategory
{
    Prefab,
    Config,
    Audio,
    Texture,
    Atlas,
    Shader,
    Other
}

public static class ResHelper
{
    public static string GetAddressableKey(E_ResourceCategory resCategory, string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new ArgumentException("Resource resId cannot be null or empty.", nameof(resourceName));

        var prefix = GetPrefixForResType(resCategory);

        // 全小写，与 AddressableAutoConfig / Manifest 完全一致
        return $"{prefix}_{resourceName}".ToLowerInvariant();
    }

    public static string GetPrefixForResType(E_ResourceCategory resCategory)
    {
        return resCategory switch
        {
            E_ResourceCategory.Prefab => "prefab",
            E_ResourceCategory.Config => "cfg",
            E_ResourceCategory.Audio => "se",
            E_ResourceCategory.Texture => "tex",
            E_ResourceCategory.Atlas => "atlas",
            E_ResourceCategory.Shader => "shader",
            _ => "asset"
        };
    }
}

/// <summary>
/// 资源加载管理器（全自动引用计数管理）
/// 仅用于启动加载或预加载！运行时请通过 GameResDB 或组件持有引用访问。
/// 所有加载的资源由 Addressables 自动管理生命周期，无需手动释放。
/// </summary>
public static class ResLoader
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
            Logger.Error( $"Failed to load asset '{key}' of resCategory {typeof(T).Name}: {ex.Message}", LogTag.Resource);
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
            Logger.Info( $"Preloaded {validKeyCount} assets of resCategory {typeof(T).Name}", LogTag.Resource);
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

public class ResManager : Singleton<ResManager>
{
    public GameResourceManifest Manifest
    {
        get
        {
            return _manifest;
        }
    }
    GameResourceManifest _manifest;
    bool _isInitialized;

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        // 加载 Manifest（它是 cfg_gameresourcemanifest）
        _manifest = await ResLoader.LoadAsync<GameResourceManifest>(
            ResHelper.GetAddressableKey(E_ResourceCategory.Config, "gameresourcemanifest")
        );
        _isInitialized = true;
    }

    // 主力加载方法：类型安全 + 自动 Key 生成
    public async Task<T> LoadAsync<T>(E_ResourceCategory resCategory, string resId) where T : UnityEngine.Object
    {
        if (!_isInitialized)
            await InitializeAsync(); // 自动初始化

        string key = ResHelper.GetAddressableKey(resCategory, resId);
        return await ResLoader.LoadAsync<T>(key);
    }

    // 预加载（支持多个）
    public async Task PreloadAsync<T>(E_ResourceCategory resCategory, params string[] names) where T : UnityEngine.Object
    {
        var keys = new string[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            keys[i] = ResHelper.GetAddressableKey(resCategory, names[i]);
        }
        await ResLoader.PreloadAsync<T>(keys);
    }

    public async Task PreloadAsync<T>(E_ResourceCategory resCategory, IList<string> names) where T : UnityEngine.Object
    {
        var keys = new string[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            keys[i] = ResHelper.GetAddressableKey(resCategory, names[i]);
        }
        await ResLoader.PreloadAsync<T>(keys);
    }
}