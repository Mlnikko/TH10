using System.Collections.Generic;

/// <summary>
/// ECS World 管理器，负责系统的注册和更新。
/// 在帧同步需求下，所有游戏逻辑系统应在 LogicTick 中运行。
/// </summary>
public class World
{
    readonly List<BaseSystem> _systems;
    readonly EntityManager _entityManager;
    readonly EntityFactory _entityFactory;
    readonly GameObjectBridge _gameObjectBridge;

    public EntityFactory EntityFactory => _entityFactory;
    public EntityManager EntityManager => _entityManager;
    public GameObjectBridge GameObjectBridge => _gameObjectBridge;

    public World()
    {
        _systems = new List<BaseSystem>();
        _entityManager = new EntityManager();
        _entityFactory = new EntityFactory(_entityManager);
        _gameObjectBridge = new GameObjectBridge();
    }

    #region 添加系统
    public T AddSystem<T>() where T : BaseSystem, new()
    {
        var system = new T();
        AddSystemInternal(system);
        return system;
    }

    public void AddSystem(BaseSystem system)
    {
        AddSystemInternal(system);
    }

    void AddSystemInternal(BaseSystem system)
    {
        system.Initialize(this);
        _systems.Add(system);
    }
    #endregion

    #region 移除系统
    public void RemoveSystem<T>() where T : BaseSystem
    {
        _systems.RemoveAll(sys =>
        {
            if (sys is T)
            {
                sys.Destroy();
                return true;
            }
            return false;
        });
    }

    #endregion

    #region 系统更新
    public void LogicTick(uint currentTick)
    {
        foreach (var sys in _systems)
        {
            if (sys.Enabled)
            {
                sys.OnLogicTick(currentTick);
            }
        }
    }

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

    public void LateUpdate(float deltaTime)
    {
        foreach (var sys in _systems)
        {
            if (sys.Enabled)
            {
                sys.OnLateUpdate(deltaTime);
            }
        }
        _gameObjectBridge.UpdateAllGameObjects(EntityManager);
    }

    #endregion

    public void Dispose()
    {
        foreach (var sys in _systems)
        {
            sys.Destroy();
        }
        _systems.Clear();
    }
}