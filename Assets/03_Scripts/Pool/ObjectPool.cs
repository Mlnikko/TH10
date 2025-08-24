using System.Collections.Generic;
using UnityEngine;
public class ObjectPool<T> where T : class
{
    int maxSize;

    readonly Stack<T> freeObjects = new();
    readonly HashSet<T> allObjects = new();

    public int FreeCount => freeObjects.Count;
    public int TotalCount => allObjects.Count;

    // 委托方法
    public delegate T FactoryMethod();
    public delegate void ObjectHandler(T obj);

    FactoryMethod createMethod;
    public ObjectHandler OnGet { get; set; }
    public ObjectHandler OnRelease { get; set; }

    public void InitPool(FactoryMethod createMethod, int initialSize = 10, int maxSize = 1000)
    {
        if (maxSize <= 0) throw new System.ArgumentOutOfRangeException(nameof(maxSize));

        this.createMethod = createMethod;
        this.maxSize = maxSize;
        Preload(initialSize);
    }

    void Preload(int count)
    {
        for (int i = 0; i < count && allObjects.Count < maxSize; i++)
        {
            T obj = CreateNew();
            freeObjects.Push(obj);
        }
    }

    T CreateNew()
    {
        T obj = createMethod();
        allObjects.Add(obj);
        return obj;
    }

    public T Get()
    {
        if (freeObjects.Count == 0 && allObjects.Count >= maxSize)
        {
            Debug.LogWarning($"{typeof(T)}池容量已达上限 {maxSize}");
            return null;
        }

        T obj = freeObjects.Count > 0 ? freeObjects.Pop() : CreateNew();
        OnGet?.Invoke(obj);
        return obj;
    }

    public void Release(T obj)
    {
        if (obj == null) throw new System.ArgumentNullException();

        if (!allObjects.Contains(obj))
            throw new System.InvalidOperationException($"{obj}不属于{typeof(T)}对象池");

        if (freeObjects.Contains(obj))
            throw new System.InvalidOperationException($"已回收{obj}");

        OnRelease?.Invoke(obj);

        if (freeObjects.Count < maxSize)
            freeObjects.Push(obj);
        else
            DestroyObject(obj);
    }

    public void DestroyObject(T obj)
    {
        allObjects.Remove(obj);

        if (obj is Component comp)
            Object.Destroy(comp.gameObject);
        else if (obj is GameObject go)
            Object.Destroy(go);
    }

    public void Clear()
    {
        foreach (var obj in allObjects)
            DestroyObject(obj);

        freeObjects.Clear();
        allObjects.Clear();
    }
}