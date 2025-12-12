using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class ResManager
{
    static readonly Dictionary<string, (object asset, AsyncOperationHandle handle)> _cache = new();
    public static async Task<T> LoadAsync<T>(string key) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Resource key cannot be null or empty.", nameof(key));

        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.asset is T result) return result;
            else throw new InvalidCastException($"Cached asset at key '{key}' is not of type {typeof(T)}.");
        }

        var handle = Addressables.LoadAssetAsync<T>(key);
        try
        {
            var op = await handle.Task;
            _cache[key] = (op, handle);
            Logger.Info($"Loaded asset: {key}", LogTag.Resource);
            return op;
        }
        catch
        {
            Addressables.Release(handle);
            throw;
        }
    }

    /// <summary>
    /// 并行预加载多个资源（自动跳过已加载项）
    /// </summary>
    /// <typeparam name="T">资源类型，必须继承 UnityEngine.Object</typeparam>
    /// <param name="keys">资源地址列表</param>
    public static async Task PreloadAsync<T>(params string[] keys) where T : UnityEngine.Object
    {
        if (keys == null || keys.Length == 0)
            return;

        var tasks = new List<Task<T>>(keys.Length);

        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key))
            {
                Logger.Warn("Skipped null or empty resource key in preload.", LogTag.Resource);
                continue;
            }

            // 如果已缓存，直接返回（避免重复加载）
            if (_cache.TryGetValue(key, out var cached))
            {
                if (cached.asset is T asset)
                {
                    tasks.Add(Task.FromResult(asset));
                }
                else
                {
                    // 类型不匹配：视为未加载，重新加载（或报错）
                    // 这里选择重新加载以保证类型正确性
                    tasks.Add(LoadAsync<T>(key));
                }
            }
            else
            {
                tasks.Add(LoadAsync<T>(key));
            }
        }

        // 并行等待所有加载完成
        await Task.WhenAll(tasks);

        Logger.Info($"Preloaded {tasks.Count} assets of type {typeof(T).Name}", LogTag.Resource);
    }

    public static void Unload(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            Addressables.Release(entry.handle);
            _cache.Remove(key);
            Logger.Info($"Unloaded asset: {key}", LogTag.Resource);
        }
    }

    public static void UnloadAll()
    {
        foreach (var entry in _cache.Values)
            Addressables.Release(entry.handle);
        _cache.Clear();
        Logger.Info("Unloaded all assets", LogTag.Resource);
    }

    public static bool IsLoaded(string key) => _cache.ContainsKey(key);

    public static T Get<T>(string key) where T : UnityEngine.Object
    {
        return _cache.TryGetValue(key, out var entry) && entry.asset is T t ? t : null;
    }
}