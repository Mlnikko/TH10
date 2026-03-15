using UnityEngine;

/// <summary>
/// 泛型MonoBehaviour单例基类
/// 支持场景手动挂载 + 代码自动创建，统一初始化
/// </summary>
public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
{
    static T _instance;
    static readonly object _lock = new object();
    static bool _isApplicationQuitting;

    [SerializeField] protected bool persistOnSceneLoad = true;
    private bool _initialized = false; // 标记是否已初始化

    public static T Instance
    {
        get
        {
            if (_isApplicationQuitting)
                return null;

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 1. 先在场景中查找
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        // 2. 场景中没有，自动创建
                        GameObject singletonObj = new GameObject($"[{typeof(T).Name}]");
                        _instance = singletonObj.AddComponent<T>();
                        // 注意：此时 Awake 会自动调用，但我们仍需确保初始化
                        // 所以在 Awake 中处理，这里不重复 init
                    }
                    // 如果找到或创建了，Instance 访问本身不触发额外初始化
                    // 初始化由 Awake 统一负责
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        // 防止重复实例
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[Singleton] 检测到重复的 {typeof(T)} 实例。销毁新实例: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        // 设置为当前实例
        _instance = this as T;

        // 只初始化一次
        if (!_initialized)
        {
            _initialized = true;
            InitializeSingleton();
        }
    }

    private void InitializeSingleton()
    {
        if (persistOnSceneLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        OnSingletonInit();
    }

    protected virtual void OnSingletonInit() { }
    protected virtual void OnSingletonDestroy() { }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            _initialized = false;
        }
        OnSingletonDestroy();
    }

    void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
        _instance = null;
    }
}