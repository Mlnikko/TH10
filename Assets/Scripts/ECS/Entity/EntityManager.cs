using System;
using System.Collections.Generic;

/// <summary>
/// 预分配临时位集：用于系统内部临时计算，避免频繁分配。
/// </summary>
public static class TempBitSets
{
    public static readonly BitSet Collision = new(EntityManager.MAX_ENTITIES);
}

/// <summary>
/// 预分配缓冲区：预分配常用的索引数组，避免每帧分配与过大数组导致的 GC 压力。
/// </summary>
public static class TempBuffers
{
    public static readonly int[] DanmakuEmitterIndices = new int[4096]; // 16KB
    public static readonly int[] DanmakuIndices = new int[16384]; // 64KB
    public static readonly int[] EnemyIndices = new int[4096];   // 16KB  
    public static readonly int[] CollisionIndices = new int[16384]; // 64KB
    public static readonly int[] CollisionActive = new int[16384];   // 用于收集活跃碰撞体
    public static readonly int[] CollisionQuery = new int[16384];   // 用于网格查询结果
}

public class EntityManager
{
    public const int MAX_ENTITIES = 65536; // 16位索引
    readonly BitSet _activeMask;
    readonly ushort[] _versions;
    readonly Queue<int> _freeIds;

    public EntityManager()
    {
        _activeMask = new BitSet(MAX_ENTITIES);
        _versions = new ushort[MAX_ENTITIES];
        _freeIds = new Queue<int>(MAX_ENTITIES);
        Initialize();
    }

    void Initialize()
    {
        for (int i = 0; i < MAX_ENTITIES; i++)
        {
            _freeIds.Enqueue(i);
            _versions[i] = 1; // 确保首个实体 Version ≥ 1
        }
    }

    public Entity CreateEntity()
    {
        if (_freeIds.Count == 0)
            throw new InvalidOperationException("Entity pool exhausted!");

        int index = _freeIds.Dequeue();

        _activeMask.Set(index, true);

        Entity entity = Entity.FromIndexAndVersion(index, _versions[index]);

        return entity;
    }
    public void DestroyEntity(Entity entity)
    {
        if (entity.IsNull) return;

        int index = entity.Index;
        ushort version = entity.Version;

        // 安全校验：防止重复销毁或销毁无效句柄
        if (index >= MAX_ENTITIES || !_activeMask.Get(index) || _versions[index] != version)
            return;

        // 销毁时递增 Version！这是防复用的关键
        _versions[index]++;
        _activeMask.Set(index, false);
        _freeIds.Enqueue(index);
    }

    // 核心安全方法：检查 Entity 是否有效
    public bool IsValid(Entity entity)
    {
        if (entity.IsNull) return false;
        return entity.Index < MAX_ENTITIES && _activeMask.Get(entity.Index) && _versions[entity.Index] == entity.Version;
    }

    // 根据索引获取实体
    public Entity GetEntity(int index)
    {
        if (index < 0 || index >= MAX_ENTITIES || !_activeMask.Get(index))
            return Entity.Null;
        return Entity.FromIndexAndVersion(index, _versions[index]);
    }

    #region AddComponent
    public void AddComponent<T>(Entity entity, in T component) where T : struct, IComponent
    {
        if (!IsValid(entity)) return;
        int index = entity.Index;
        AddComponent<T>(index, component);
    }

    public void AddComponent<T>(int index, in T component) where T : struct, IComponent
    {
        if ((uint)index >= MAX_ENTITIES || !_activeMask.Get(index)) return;
        ComponentStorage<T>.Add(index, component); // 调用新的 Add
    }
    #endregion

    #region RemoveComponent
    public void RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsValid(entity)) return;
        int index = entity.Index;
        RemoveComponent<T>(index);
    }

    public void RemoveComponent<T>(int index) where T : struct, IComponent
    {
        if ((uint)index >= MAX_ENTITIES || !_activeMask.Get(index)) return;
        ComponentStorage<T>.Remove(index);
    }
    #endregion

    public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsValid(entity))
            throw new ArgumentException("Invalid entity");
        return ref ComponentStorage<T>.Components[entity.Index];
    }

    public bool HasComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsValid(entity)) return false;
        return ComponentStorage<T>.HasComponent.Get(entity.Index);
    }

    public Span<T> GetComponentSpan<T>() where T : struct, IComponent
    {
        return ComponentStorage<T>.Components.AsSpan();
    }

    public Span<int> GetActiveIndices<T>() where T : struct, IComponent
    {
        return ComponentStorage<T>.GetActiveIndices();
    }
}