
/// <summary>
/// 所有 ECS 系统的基类，提供统一生命周期和更新控制。
/// </summary>
public abstract class BaseSystem
{
    protected World World { get; private set; }
    protected EntityManager EntityManager
    {
        get
        {
            if (World == null) throw new System.InvalidOperationException("World is not initialized.");

            return World.EntityManager;
        }
    }
    /// <summary>
    /// 系统是否启用（可动态开关）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 初始化系统（由 World 调用）
    /// </summary>
    public void Initialize(World world)
    {
        World = world;
        OnCreate();
    }

    /// <summary>
    /// 销毁系统（清理资源）
    /// </summary>
    public void Destroy()
    {
        OnDestroy();
        World = null;
    }

    // ------------------ 可选生命周期钩子 ------------------

    /// <summary>
    /// 系统创建时调用（替代构造函数）
    /// </summary>
    protected virtual void OnCreate() { }

    /// <summary>
    /// 系统销毁前调用
    /// </summary>
    protected virtual void OnDestroy() { }

    // ------------------ 更新接口 ------------------

    /// <summary>
    /// 每帧更新（用于输入、UI等）
    /// </summary>
    public virtual void OnUpdate(float deltaTime) { }

    /// <summary>
    /// 逻辑帧更新（用于帧同步）
    /// </summary>
    public virtual void OnLogicTick(uint tick) { }

    /// <summary>
    /// 每帧更新（用于渲染、UI等）
    /// </summary>
    public virtual void OnLateUpdate(float deltaTime) { }
}