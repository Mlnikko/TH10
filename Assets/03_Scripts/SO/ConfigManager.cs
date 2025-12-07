using System;
using System.Collections.Generic;
using UnityEngine;

public static class ConfigManager
{
    // 缓存字典：Type + ID → Config
    private static readonly Dictionary<(Type, string), GameConfig> _cache = new();

    // 可配置的加载根路径（方便后期切 Addressables）
    private const string CONFIG_ROOT = "Configs";

    /// <summary>
    /// 获取配置（线程安全，仅主线程调用）
    /// </summary>
    public static T Get<T>(string id) where T : GameConfig
    {
        var key = (typeof(T), id);
        if (_cache.TryGetValue(key, out var config))
        {
            return (T)config;
        }

        // 首次加载
        var path = $"{CONFIG_ROOT}/{typeof(T).Name}/{id}";
        var asset = Resources.Load<T>(path);
        if (asset == null)
        {
            Debug.LogError($"Config not found: {path}");
            return null;
        }

        _cache[key] = asset;
        return asset;
    }

    /// <summary>
    /// 预加载所有指定类型的配置（启动时调用）
    /// </summary>
    public static void PreloadAll<T>() where T : GameConfig
    {
        var path = $"{CONFIG_ROOT}/{typeof(T).Name}";
        var assets = Resources.LoadAll<T>(path);
        foreach (var asset in assets)
        {
            var key = (typeof(T), asset.ConfigId);
            _cache[key] = asset;
        }
        Logger.Info($"Preloaded {assets.Length} configs of type {typeof(T).Name}", LogTag.Resource);
    }

    /// <summary>
    /// 清空缓存（用于场景切换或热更）
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
    }
}