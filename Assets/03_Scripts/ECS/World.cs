// World.cs
using System.Collections.Generic;

/// <summary>
/// ECS World 管理器，负责系统的注册和更新。
/// 在帧同步需求下，所有游戏逻辑系统应在 FixedUpdate 中运行。
/// </summary>
public class World
{
    private readonly List<BaseSystem> _systems = new();
    private readonly EntityManager _entityManager = new();

    public EntityManager EntityManager => _entityManager;

    public T AddSystem<T>() where T : BaseSystem, new()
    {
        var system = new T();
        system.Initialize(_entityManager);
        _systems.Add(system);
        return system;
    }
    
    /// <summary>
    /// 获取已注册的系统
    /// </summary>
    public T GetSystem<T>() where T : BaseSystem
    {
        foreach (var sys in _systems)
        {
            if (sys is T result)
            {
                return result;
            }
        }
        return null;
    }

    /// <summary>
    /// 固定时间步长更新（用于帧同步）
    /// 所有游戏逻辑系统（移动、碰撞、生命周期等）应在此方法中更新
    /// 重要：必须使用 Time.fixedDeltaTime 作为参数，确保所有客户端使用相同的固定时间步长
    /// </summary>
    public void FixedUpdate(float fixedDeltaTime)
    {
        foreach (var sys in _systems)
        {
            if (sys.Enabled)
            {
                sys.OnFixedUpdate(fixedDeltaTime);
            }
        }
    }

    /// <summary>
    /// 每帧更新（用于非逻辑系统更新）
    /// 注意：帧同步游戏应避免在此方法中执行游戏逻辑
    /// 所有影响游戏状态的计算都应在 FixedUpdate 中进行
    /// </summary>
    public void Update(float deltaTime)
    {
        foreach (var sys in _systems)
        {
            if (sys.Enabled)
            {
                sys.OnUpdate(deltaTime);
            }
        }
    }

    /// <summary>
    /// 每帧更新（用于渲染、UI等）
    /// </summary>
    /// <param name="deltaTime"></param>
    public void LateUpdate(float deltaTime)
    {
        foreach (var sys in _systems)
        {
            if (sys.Enabled)
            {
                sys.OnLateUpdate(deltaTime);
            }
        }
    }

    public void Dispose()
    {
        foreach (var sys in _systems)
        {
            sys.Destroy();
        }
        _systems.Clear();
    }
}