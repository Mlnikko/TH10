using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnGet();  // 获取时调用
    void OnReturn(); // 回收时调用
}

public class GameObjectPoolManager : SingletonMono<GameObjectPoolManager>
{
    // 【核心优化】直接使用数组！
    // 下标 = prefabIndex。访问速度：CPU 指令级，无 Hash，无分配。
    Queue<GameObject>[] _pools;
    Transform[] _poolRoots; // 每个池对应的父节点
    // 反向映射：GameObject -> prefabIndex
    // 由于回收时只需要知道 index，这个 Dictionary 无法避免，但只在 Return 时用一次
    Dictionary<GameObject, int> _goToIndexMap;

    // 记录最大索引，用于边界检查
    int _maxPrefabIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        _goToIndexMap = new Dictionary<GameObject, int>(1024);
    }

    /// <summary>
    /// 初始化池数组 (必须在 Warmup 之前调用)
    /// </summary>
    /// <param name="maxIndex">最大的 prefabIndex 值</param>
    public void Initialize(int maxIndex)
    {
        if (_pools != null) return; // 防止重复初始化

        _maxPrefabIndex = maxIndex;
        int size = maxIndex + 1;

        // 创建数组，长度为 maxIndex + 1 (因为索引从 0 开始)
        _pools = new Queue<GameObject>[size];
        _poolRoots = new Transform[size];
        Logger.Info($"GameObjectPoolManager initialized for indices 0 to {maxIndex}");
    }

    /// <summary>
    /// 预热指定索引的池
    /// </summary>
    public void WarmupPool(int prefabIndex, int count)
    {
        if (_pools == null)
        {
            Logger.Error("Pool not initialized! Call Initialize() first.");
            return;
        }

        if (prefabIndex < 0 || prefabIndex > _maxPrefabIndex)
        {
            Logger.Error($"Invalid prefabIndex: {prefabIndex}. Max allowed: {_maxPrefabIndex}");
            return;
        }

        // 如果已经预热过，跳过
        if (_pools[prefabIndex] != null)
        {
            Logger.Warn($"Pool {prefabIndex} already warmed up.");
            return;
        }

        // 获取预制体 (GameResDB 内部也是数组访问，极速)
        GameObject prefab = GameResDB.Instance.GetPrefab(prefabIndex);
        if (prefab == null)
        {
            Logger.Error($"Prefab at index {prefabIndex} is missing in GameResDB!");
            return;
        }

        // 初始化队列
        var queue = new Queue<GameObject>(count);
        _pools[prefabIndex] = queue;

        Transform root = GetOrCreatePoolRoot(prefabIndex, prefab.name);
        _poolRoots[prefabIndex] = root;


        for (int i = 0; i < count; i++)
        {
            CreateAndEnqueue(prefabIndex, prefab, queue, root);
        }

        Logger.Info($"Pool Warmed: Index[{prefabIndex}] x{count}");
    }

    void CreateAndEnqueue(int prefabIndex, GameObject prefab, Queue<GameObject> queue, Transform root)
    {
        var obj = Instantiate(prefab, root); // 直接指定父节点
        obj.SetActive(false);
        obj.name = prefab.name; // 保持名字清晰

        // 可选：给池内物体加个标记，方便区分
        // obj.hideFlags = HideFlags.DontSaveInEditor; 

        _goToIndexMap[obj] = prefabIndex;
        obj.GetComponent<IPoolable>()?.OnReturn();
        queue.Enqueue(obj);
    }

    /// <summary>
    /// 获取对象 (极速数组访问)
    /// </summary>
    public GameObject Get(int prefabIndex)
    {
        if (_pools == null || prefabIndex < 0 || prefabIndex > _maxPrefabIndex)
        {
            Logger.Error($"Invalid get request for index: {prefabIndex}");
            return null;
        }

        var queue = _pools[prefabIndex];

        // 检查池是否为空
        if (queue == null || queue.Count == 0)
        {
            Logger.Error($"POOL EXHAUSTED! Index: {prefabIndex}. Increase warmup count!");
            return null;
        }

        var obj = queue.Dequeue();      

        obj.transform.SetParent(null); // 移出池容器

        obj.GetComponent<IPoolable>()?.OnGet();
        return obj;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        // 查找所属索引 (这是唯一一次 Dictionary 查找，不可避免，但频率远低于 Get)
        if (!_goToIndexMap.TryGetValue(obj, out int prefabIndex))
        {
            Logger.Warn($"Returning unmanaged object: {obj.name}. Destroying.");
            Destroy(obj);
            return;
        }

        // 状态重置
        obj.GetComponent<IPoolable>()?.OnReturn();

        obj.SetActive(false);

        if (prefabIndex <= _maxPrefabIndex && _poolRoots[prefabIndex] != null)
        {
            obj.transform.SetParent(_poolRoots[prefabIndex]);
            _pools[prefabIndex].Enqueue(obj);
        }
        else
        {
            Logger.Error($"Cannot return to invalid pool index: {prefabIndex}");
            _goToIndexMap.Remove(obj);
            Destroy(obj);
        }
    }

    /// <summary>
    /// 清理所有池 (关卡结束)
    /// </summary>
    public void ClearAllPools()
    {
        if (_pools == null) return;

        for (int i = 0; i < _pools.Length; i++)
        {
            if (_pools[i] == null) continue;

            while (_pools[i].Count > 0)
            {
                var obj = _pools[i].Dequeue();
                _goToIndexMap.Remove(obj);
                Destroy(obj);
            }
            _pools[i] = null;
        }

        _goToIndexMap.Clear();
        Logger.Info("All pools cleared.");
    }

    Transform GetOrCreatePoolRoot(int prefabIndex, string prefabName)
    {
        if (_poolRoots[prefabIndex] != null)
            return _poolRoots[prefabIndex];

        // 尝试从配置获取更友好的分类名 (例如 "Danmaku", "Enemy")
        // 这里简单处理：提取预制体名字的前缀，或者统一命名
        // 假设预制体叫 "Danmaku_Bullet_01"，我们可以提取 "Danmaku" 作为文件夹名
        string categoryName = ExtractCategoryName(prefabName);

        string rootName = $"[Pool] {categoryName} (Idx:{prefabIndex})";

        var rootObj = new GameObject(rootName);
        rootObj.transform.SetParent(transform); // 挂在 Manager 下面
        rootObj.SetActive(true); // 父节点必须激活，子物体才能被实例化（即使子物体是 inactive）

        _poolRoots[prefabIndex] = rootObj.transform;
        return rootObj.transform;
    }
    string ExtractCategoryName(string prefabName)
    {
        if (prefabName.StartsWith("DM")) return "Danmaku";

        // 默认返回前缀直到第一个 '_' 或整个名字
        int underscoreIndex = prefabName.IndexOf('_');
        if (underscoreIndex > 0)
            return prefabName.Substring(0, underscoreIndex);

        return "Misc";
    }
}