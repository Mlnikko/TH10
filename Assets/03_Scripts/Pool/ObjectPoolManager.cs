// ObjectPoolManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : SingletonMono<ObjectPoolManager>
{
    private readonly Dictionary<string, object> pools = new();

    // 自动回收协程
    public IEnumerator AutoDespawnRoutine<T>(T obj, float delay, ObjectPool<T> pool) where T : Component
    {
        yield return new WaitForSeconds(delay);
        pool.Return(obj);
    }

    /// <summary>
    /// 获取或创建对象池（首次调用会自动 Warmup）
    /// </summary>
    public ObjectPool<T> GetPool<T>(
        string assetKey,
        int initialSize = 5,
        int maxSize = 20,
        bool autoRelease = false,
        float autoReleaseDelay = 3f) where T : Component
    {
        string key = $"{typeof(T).Name}_{assetKey}";
        if (!pools.TryGetValue(key, out var pool))
        {
            var newPool = new ObjectPool<T>(
                assetKey,
                transform,
                initialSize,
                maxSize,
                autoRelease,
                autoReleaseDelay
            );
            newPool.WarmupAsync(); // 异步预热
            pools[key] = newPool;
            pool = newPool;
        }
        return (ObjectPool<T>)pool;
    }

    /// <summary>
    /// 手动预热某个池（可用于关卡加载时）
    /// </summary>
    public void WarmupPool<T>(
        string assetKey,
        Action onComplete = null,
        int initialSize = 5,
        int maxSize = 20,
        bool autoRelease = false,
        float autoReleaseDelay = 3f) where T : Component
    {
        var pool = GetPool<T>(assetKey, initialSize, maxSize, autoRelease, autoReleaseDelay);
        // Warmup 已在 GetPool 中触发，这里可等待完成（如需同步）
        onComplete?.Invoke();
    }

    /// <summary>
    /// 清理所有池（切换场景时调用）
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in pools.Values)
        {
            if (pool is ObjectPool<Component> genericPool)
            {
                // 反射调用 Clear（或设计为非泛型基类）
                // 简化：此处略，实际项目建议用非泛型基类
            }
        }
        pools.Clear();
    }
}