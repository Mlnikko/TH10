using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class ObjectPoolManager
{
    static readonly Dictionary<string, GameObjectPool> _pools = new();

    /// <summary>
    /// 初始化所有池（在游戏启动或关卡加载时调用，异步加载 prefab 在此完成）
    /// </summary>
    public static async Task InitializePoolsAsync(Dictionary<string, int> poolConfigs)
    {
        // 1. 并行加载所有 prefab
        var prefabTasks = new Dictionary<string, Task<GameObject>>();
        foreach (var key in poolConfigs.Keys)
        {
            prefabTasks[key] = ResManager.LoadAsync<GameObject>(key);
        }

        await Task.WhenAll(prefabTasks.Values);

        // 2. 同步创建池（此时所有 prefab 已就绪）
        foreach (var kvp in poolConfigs)
        {
            string key = kvp.Key;
            int capacity = kvp.Value;
            GameObject prefab = await prefabTasks[key];

            if (prefab != null)
            {
                _pools[key] = new GameObjectPool(key, prefab, capacity);
            }
        }
    }

    // 同步接口（供逻辑系统使用）
    public static GameObject Spawn(string prefabKey)
    {
        return _pools.TryGetValue(prefabKey, out var pool) ? pool.Get() : null;
    }

    public static void Despawn(GameObject obj, string prefabKey)
    {
        if (_pools.TryGetValue(prefabKey, out var pool))
        {
            pool.Return(obj);
        }
        else
        {
            UnityEngine.Object.Destroy(obj); // 安全兜底
        }
    }
}