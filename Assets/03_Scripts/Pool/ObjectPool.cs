// ObjectPool.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型对象池，适用于 GameObject 或 Component
/// </summary>
public class ObjectPool<T> where T : Component
{
    readonly string assetKey; // Addressable Key
    readonly Transform poolRoot;
    readonly Queue<T> inactiveObjects = new();
    readonly List<T> activeObjects = new(); // 用于调试或强制回收

    private readonly T prefab;
    private readonly int initialSize;
    private readonly int maxSize;
    private readonly bool autoRelease; // 是否自动回收（如特效）
    private readonly float autoReleaseDelay; // 自动回收延迟（秒）

    public int ActiveCount => activeObjects.Count;
    public int InactiveCount => inactiveObjects.Count;

    public ObjectPool(
        string assetKey,
        Transform parent = null,
        int initialSize = 5,
        int maxSize = 20,
        bool autoRelease = false,
        float autoReleaseDelay = 3f)
    {
        this.assetKey = assetKey;
        this.initialSize = initialSize;
        this.maxSize = maxSize;
        this.autoRelease = autoRelease;
        this.autoReleaseDelay = autoReleaseDelay;

        poolRoot = new GameObject($"Pool_{typeof(T).Name}_{assetKey}").transform;
        poolRoot.SetParent(parent ?? ObjectPoolManager.Instance.transform, false);
        poolRoot.gameObject.SetActive(false); // 根节点隐藏，子对象按需激活
    }

    /// <summary>
    /// 异步预热池（加载 Prefab 并初始化对象）
    /// </summary>
    public void WarmupAsync(Action onComplete = null)
    {
        if (prefab != null)
        {
            Preallocate(initialSize);
            onComplete?.Invoke();
            return;
        }

        //ResManager.LoadAsync(assetKey, go =>
        //{
        //    if (go == null)
        //    {
        //        Debug.LogError($"[ObjectPool] Failed to load prefab: {assetKey}");
        //        onComplete?.Invoke();
        //        return;
        //    }

        //    prefab = go.GetComponent<T>();
        //    if (prefab == null)
        //    {
        //        Debug.LogError($"[ObjectPool] Prefab missing component {typeof(T)}: {assetKey}");
        //        onComplete?.Invoke();
        //        return;
        //    }

        //    Preallocate(initialSize);
        //    onComplete?.Invoke();
        //});
    }

    void Preallocate(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var obj = CreateInstance();
            Return(obj);
        }
    }

    T CreateInstance()
    {
        var go = GameObject.Instantiate(prefab.gameObject, poolRoot);
        var comp = go.GetComponent<T>() ?? go.AddComponent<T>();
        go.SetActive(false);
        return comp;
    }

    /// <summary>
    /// 从池中获取对象
    /// </summary>
    public T Get(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogError($"[ObjectPool] Prefab not loaded yet! Call WarmupAsync first. Key: {assetKey}");
            return null;
        }

        T instance;
        if (inactiveObjects.Count > 0)
        {
            instance = inactiveObjects.Dequeue();
        }
        else if (activeObjects.Count < maxSize)
        {
            instance = CreateInstance();
        }
        else
        {
            Debug.LogWarning($"[ObjectPool] Max size reached ({maxSize}) for {typeof(T)}:{assetKey}. Reusing oldest.");
            instance = activeObjects[0];
            Return(instance);
            instance = Get(position, rotation, parent); // 递归一次
            return instance;
        }

        // 设置位置/旋转/父节点
        instance.transform.SetPositionAndRotation(position, rotation);
        if (parent != null)
            instance.transform.SetParent(parent, true);

        instance.gameObject.SetActive(true);

        // 回调
        if (instance is IRecyclable recyclable)
            recyclable.OnSpawn();

        activeObjects.Add(instance);

        // 自动回收
        if (autoRelease && autoReleaseDelay > 0)
        {
            ObjectPoolManager.Instance.StartCoroutine(
                ObjectPoolManager.Instance.AutoDespawnRoutine(instance, autoReleaseDelay, this)
            );
        }

        return instance;
    }

    /// <summary>
    /// 手动回收对象
    /// </summary>
    public void Return(T instance)
    {
        if (instance == null || !activeObjects.Contains(instance)) return;

        activeObjects.Remove(instance);

        if (instance is IRecyclable recyclable)
            recyclable.OnDespawn();

        instance.gameObject.SetActive(false);
        instance.transform.SetParent(poolRoot, false);

        if (inactiveObjects.Count < maxSize)
            inactiveObjects.Enqueue(instance);
        else
            GameObject.Destroy(instance.gameObject); // 超出最大缓存，直接销毁
    }

    /// <summary>
    /// 清空池（卸载所有对象）
    /// </summary>
    public void Clear()
    {
        foreach (var obj in activeObjects)
            GameObject.Destroy(obj.gameObject);
        foreach (var obj in inactiveObjects)
            GameObject.Destroy(obj.gameObject);

        activeObjects.Clear();
        inactiveObjects.Clear();
    }
}