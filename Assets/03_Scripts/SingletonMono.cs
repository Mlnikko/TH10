using UnityEngine;

/// <summary>
/// 泛型MonoBehaviour单例基类
/// 特性：
/// 1. 自动创建实例（首次访问时）
/// 2. 跨场景持久化（可选）
/// 3. 线程安全初始化
/// 4. 防止重复实例
/// </summary>
public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
    static T _instance;
    static readonly object _lock = new object();
    static bool _isApplicationQuitting;
    [SerializeField] protected bool persistOnSceneLoad = true;

    public static T Instance
    {
        get
        {
            if (_isApplicationQuitting)
            {
                Debug.LogWarning($"[Singleton] {typeof(T)} 实例已被销毁，因为应用已退出。返回空值。");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 场景中查找现有实例
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        // 创建新实例
                        GameObject singletonObj = new GameObject($"{typeof(T).Name} (Singleton)");
                        _instance = singletonObj.AddComponent<T>();

                        // 初始化单例
                        _instance.InitializeSingleton();

                        Debug.Log($"[Singleton] 创建 {typeof(T)} 单例实例");
                    }
                    else
                    {
                        // 初始化已存在的实例
                        _instance.InitializeSingleton();
                    }
                }
                return _instance;
            }
        }
    }
    void InitializeSingleton()
    {
        // 确保单例持久化
        if (persistOnSceneLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
        // 子类自定义初始化
        OnSingletonInit();
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            InitializeSingleton();
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] 检测到重复的 {typeof(T)} 实例。销毁新实例: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    protected virtual void OnSingletonInit() { }
    protected virtual void OnSingletonDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    protected virtual void OnDestroy()
    {
        OnSingletonDestroy();
    }

    void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
        _instance = null;
    }
}