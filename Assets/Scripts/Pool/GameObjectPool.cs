using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 单一类型的 GameObject 对象池（内部类，不直接暴露）
/// </summary>
public class GameObjectPool
{
    private readonly string _prefabKey;
    private readonly Queue<GameObject> _pool = new();
    private readonly GameObject _prefab; // 必须在构造时传入（已加载）

    public GameObjectPool(string prefabKey, GameObject prefab, int capacity)
    {
        _prefabKey = prefabKey;
        _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));

        // 预热：同步创建所有实例（在主线程，非逻辑 tick）
        for (int i = 0; i < capacity; i++)
        {
            var obj = UnityEngine.Object.Instantiate(_prefab);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// 同步获取对象（无 await，无 Instantiate）
    /// </summary>
    public GameObject Get()
    {
        if (_pool.Count == 0)
        {
            Logger.Warn( $"Pool exhausted: {_prefabKey}");
            return null; // 或抛异常
        }

        var obj = _pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }

    public void Dispose()
    {
        while (_pool.Count > 0)
        {
            UnityEngine.Object.Destroy(_pool.Dequeue());
        }
    }
}