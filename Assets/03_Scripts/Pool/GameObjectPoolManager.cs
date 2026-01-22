using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class ObjectPoolManager
{
    private static readonly Dictionary<string, GameObjectPool> _pools = new();

    /// <summary>
    /// 创建或获取一个对象池（建议在关卡开始前调用）
    /// </summary>
    /// <param name="prefabKey">Addressable 资源 Key</param>
    /// <param name="initialCapacity">初始预热数量</param>
    public static void CreatePool(string prefabKey, int initialCapacity = 10)
    {
        if (_pools.ContainsKey(prefabKey))
        {
            Logger.Warn($"Pool for {prefabKey} already exists!", LogTag.Pool);
            return;
        }

        var pool = new GameObjectPool(prefabKey, initialCapacity);
        _pools[prefabKey] = pool;
    }

    /// <summary>
    /// 异步获取对象（推荐用于运行时发射弹幕等）
    /// </summary>
    public static async Task<GameObject> SpawnAsync(string prefabKey, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!_pools.TryGetValue(prefabKey, out var pool))
        {
            // 自动创建池（懒加载），但不预热（可能有性能抖动）
            Logger.Warn($"Pool not created for {prefabKey}, creating on-demand!", LogTag.Pool);
            pool = new GameObjectPool(prefabKey);
            _pools[prefabKey] = pool;
        }

        return await pool.GetAsync(position, rotation, parent);
    }

    /// <summary>
    /// 回收对象（必须配对使用！）
    /// </summary>
    public static void Despawn(GameObject obj, string prefabKey)
    {
        if (_pools.TryGetValue(prefabKey, out var pool))
        {
            pool.Return(obj);
        }
        else
        {
            // 安全兜底：如果没池，直接 Destroy（但应避免）
            Logger.Warn($"No pool found for {prefabKey}, destroying object directly!", LogTag.Pool);
            UnityEngine.Object.Destroy(obj);
        }
    }

    /// <summary>
    /// 批量预热多个池（适合关卡加载时调用）
    /// </summary>
    public static void PreWarmPools(params (string key, int count)[] configs)
    {
        foreach (var (key, count) in configs)
        {
            CreatePool(key, count);
        }
    }

    /// <summary>
    /// 清理所有池（切换场景时调用）
    /// </summary>
    public static void ClearAllPools()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Dispose();
        }
        _pools.Clear();
        Logger.Info("All pools cleared", LogTag.Pool);
    }
}