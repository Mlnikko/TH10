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
    readonly LogicFrameDriver _logicFrameTimer;

    public EntityFactory EntityFactory => _entityFactory;
    public EntityManager EntityManager => _entityManager;
    public GameObjectBridge GameObjectBridge => _gameObjectBridge;
    public LogicFrameDriver LogicFrameTimer => _logicFrameTimer;

    public World()
    {
        _systems = new List<BaseSystem>();
        _entityManager = new EntityManager();
        _logicFrameTimer = new LogicFrameDriver(GameManager.logicFPS);
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

    /// <summary>
    /// 按注册顺序查找已添加的系统（找不到时返回 null）。
    /// </summary>
    public T GetSystem<T>() where T : BaseSystem
    {
        for (int i = 0; i < _systems.Count; i++)
        {
            if (_systems[i] is T match)
                return match;
        }
        return null;
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
    public void LogicTick(uint currentframe)
    {
        for (int i = 0; i < _systems.Count; i++)
        {
            var sys = _systems[i];
            if (sys.Enabled)
            {
                sys.OnLogicTick(currentframe);
            }
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = 0; i < _systems.Count; i++)
        {
            var sys = _systems[i];
            if (sys.Enabled)
            {
                sys.OnUpdate(deltaTime);
            }
        }
    }

    public void LateUpdate(float deltaTime)
    {
        for (int i = 0; i < _systems.Count; i++)
        {
            var sys = _systems[i];
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
        for (int i = 0; i < _systems.Count; i++)
        {
            var sys = _systems[i];
            sys.Destroy();
        }
        _systems.Clear();
    }
}