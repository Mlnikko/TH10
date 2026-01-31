// ObjectPoolManager.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ObjectPoolManager : SingletonMono<ObjectPoolManager>
{
    Dictionary<string, Queue<GameObject>> _pools = new();
    Dictionary<string, GameObject> _prefabCache = new();

    // 预热指定 Prefab 到对象池
    public async Task WarmupPoolAsync(string prefabId, int count)
    {
        if (_pools.ContainsKey(prefabId))
            return; // 已预热

        var prefab = await ResLoader.LoadAsync<GameObject>(prefabId);
        if (prefab == null)
        {
            Logger.Warn($"Failed to load prefab for pooling: {prefabId}");
            return;
        }

        _prefabCache[prefabId] = prefab;
        _pools[prefabId] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform); // 防止场景污染
            _pools[prefabId].Enqueue(obj);
        }

        Logger.Info($"Object pool warmed up: {prefabId} x{count}");
    }

    // 获取对象（从池中或新建）
    public GameObject GetFromPool(string prefabId)
    {
        if (!_pools.TryGetValue(prefabId, out var pool) || pool.Count == 0)
        {
            // fallback: 动态创建（可用于调试，但应避免运行时发生）
            var prefab = _prefabCache.GetValueOrDefault(prefabId);
            if (prefab == null)
            {
                Logger.Error($"Prefab not preloaded for pool: {prefabId}");
                return null;
            }
            return Instantiate(prefab);
        }

        var obj = pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    // 回收对象
    public void ReturnToPool(string prefabId, GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(transform);

        if (_pools.TryGetValue(prefabId, out var pool))
        {
            pool.Enqueue(obj);
        }
        else
        {
            // 如果未预热，可选择自动创建池（不推荐），或直接 Destroy
            Destroy(obj);
        }
    }

    // 清理所有池（战斗结束时调用）
    public void ClearAllPools()
    {
        foreach (var kvp in _pools)
        {
            while (kvp.Value.Count > 0)
            {
                Destroy(kvp.Value.Dequeue());
            }
        }
        _pools.Clear();
        _prefabCache.Clear();
    }
}