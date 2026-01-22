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
    private readonly Transform _poolRoot;
    private readonly Queue<GameObject> _pool = new();
    private GameObject _prefab; // 缓存 prefab 引用（来自 ResManager）

    public GameObjectPool(string prefabKey, int initialCapacity = 0)
    {
        _prefabKey = prefabKey;
        _poolRoot = new GameObject($"[Pool] {prefabKey}").transform;
        _poolRoot.gameObject.SetActive(false); // 根节点禁用，不影响子物体激活状态

        if (initialCapacity > 0)
        {
            PreWarm(initialCapacity);
        }
    }

    /// <summary>
    /// 预热：预先生成 N 个实例放入池中（需确保 prefab 已加载）
    /// </summary>
    public async void PreWarm(int count)
    {
        if (_prefab == null)
        {
            _prefab = await PrefabManager.LoadPrefabAsync(_prefabKey);
            if (_prefab == null)
            {
                Logger.Error($"Failed to preload prefab: {_prefabKey}", LogTag.Pool);
                return;
            }
        }

        for (int i = 0; i < count; i++)
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _poolRoot);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
        Logger.Info($"Pre-warmed {_prefabKey} with {count} instances", LogTag.Pool);
    }

    /// <summary>
    /// 获取一个可用对象（自动加载 prefab 若未缓存）
    /// </summary>
    public async Task<GameObject> GetAsync(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (_prefab == null)
        {
            _prefab = await PrefabManager.LoadPrefabAsync(_prefabKey);
            if (_prefab == null)
            {
                Logger.Error($"Failed to load prefab: {_prefabKey}", LogTag.Pool);
                return null;
            }
        }

        GameObject obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
        }
        else
        {
            // 池空，动态创建（应尽量避免，可通过预热减少）
            obj = UnityEngine.Object.Instantiate(_prefab, _poolRoot);
            Logger.Warn($"Pool exhausted for {_prefabKey}, creating new instance!", LogTag.Pool);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(parent, false);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 回收对象到池（必须由使用者调用！）
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        obj.transform.SetParent(_poolRoot, false);
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }

    /// <summary>
    /// 清空池（释放所有实例，但保留 prefab 引用）
    /// </summary>
    public void Clear()
    {
        while (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
            UnityEngine.Object.Destroy(obj);
        }
    }

    /// <summary>
    /// 完全销毁池（包括 prefab 缓存）
    /// </summary>
    public void Dispose()
    {
        Clear();
        UnityEngine.Object.Destroy(_poolRoot.gameObject);
        // 注意：不主动 Unload prefab，交由 ResManager 统一管理生命周期
    }
}