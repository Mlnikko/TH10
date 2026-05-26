using System;
using System.Collections.Generic;

/// <summary>
/// 预分配临时位集：用于系统内部临时计算，避免频繁分配。
/// </summary>
public static class TempBitSets
{
    public static readonly BitSet Collision = new(EntityManager.MAX_ENTITIES);
    /// <summary>插入网格时是否使用了玩家弹幕扫掠 AABB；查询阶段复用 <see cref="TempBuffers.CollisionSweptAabb"/>，避免重复粗测计算。</summary>
    public static readonly BitSet CollisionSweptBroadphase = new(EntityManager.MAX_ENTITIES);
    /// <summary>本逻辑帧内已结算过「击中敌人」的玩家弹幕实体索引，防止同一帧重复命中。</summary>
    public static readonly BitSet PlayerDanmakuHitConsumed = new(EntityManager.MAX_ENTITIES);
    /// <summary>本逻辑帧内已被拾取的掉落物实体索引，防止同一帧重复触发效果。</summary>
    public static readonly BitSet DropItemPickupConsumed = new(EntityManager.MAX_ENTITIES);
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

    /// <summary>与 <see cref="TempBitSets.CollisionSweptBroadphase"/> 配套，按实体索引存储扫掠粗测 AABB。</summary>
    public static readonly float[] CollisionSweptAabbMinX = new float[EntityManager.MAX_ENTITIES];
    public static readonly float[] CollisionSweptAabbMinY = new float[EntityManager.MAX_ENTITIES];
    public static readonly float[] CollisionSweptAabbMaxX = new float[EntityManager.MAX_ENTITIES];
    public static readonly float[] CollisionSweptAabbMaxY = new float[EntityManager.MAX_ENTITIES];
}

public class EntityManager
{
    public const int MAX_ENTITIES = 65536; // 16位索引
    readonly BitSet _activeMask;
    readonly ushort[] _versions;
    readonly uint[] _componentMasks;
    readonly Queue<int> _freeIds;

    public EntityManager()
    {
        _activeMask = new BitSet(MAX_ENTITIES);
        _versions = new ushort[MAX_ENTITIES];
        _componentMasks = new uint[MAX_ENTITIES];
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

        uint mask = _componentMasks[index];
        if (mask != 0)
            ClearComponentsFromMask(index, mask);

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

        ClearComponentsFromMask(index, _componentMasks[index]);

        // 销毁时递增 Version！这是防复用的关键
        _versions[index]++;
        _activeMask.Set(index, false);
        _freeIds.Enqueue(index);
    }

    /// <summary>实体槽位是否仍被分配（未 Destroy）。</summary>
    public bool IsIndexActive(int index) =>
        (uint)index < MAX_ENTITIES && _activeMask.Get(index);

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

        uint bit = ComponentMaskBits.GetMask<T>();
        if (bit == 0)
            return;

        ComponentStorage<T>.Add(index, component);
        _componentMasks[index] |= bit;
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
        RemoveComponentAtSlot<T>(index);
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
        return HasComponent<T>(entity.Index);
    }

    public bool HasComponent<T>(int index) where T : struct, IComponent
    {
        if (!IsIndexActive(index)) return false;
        uint bit = ComponentMaskBits.GetMask<T>();
        return bit != 0 && (_componentMasks[index] & bit) != 0;
    }

    public Span<T> GetComponentSpan<T>() where T : struct, IComponent
    {
        return ComponentStorage<T>.Components.AsSpan();
    }

    public Span<int> GetActiveIndices<T>() where T : struct, IComponent
    {
        return ComponentStorage<T>.GetActiveIndices();
    }

    void ClearComponentsFromMask(int index, uint mask)
    {
        if (mask == 0)
            return;

        if ((mask & ComponentMaskBits.CPoolRecycleTag) != 0) RemoveComponentAtSlot<CPoolRecycleTag>(index);
        if ((mask & ComponentMaskBits.CPoolGetTag) != 0) RemoveComponentAtSlot<CPoolGetTag>(index);
        if ((mask & ComponentMaskBits.CNoOffscreenRecycleTag) != 0) RemoveComponentAtSlot<CNoOffscreenRecycleTag>(index);
        if ((mask & ComponentMaskBits.CGameObjectLink) != 0) RemoveComponentAtSlot<CGameObjectLink>(index);
        if ((mask & ComponentMaskBits.CPresentationPose) != 0) RemoveComponentAtSlot<CPresentationPose>(index);

        if ((mask & ComponentMaskBits.CPlayerEmitterOwnership) != 0) RemoveComponentAtSlot<CPlayerEmitterOwnership>(index);
        if ((mask & ComponentMaskBits.CDanmakuEmitter) != 0) RemoveComponentAtSlot<CDanmakuEmitter>(index);
        if ((mask & ComponentMaskBits.CDanmaku) != 0) RemoveComponentAtSlot<CDanmaku>(index);
        if ((mask & ComponentMaskBits.CDropItemMagnet) != 0) RemoveComponentAtSlot<CDropItemMagnet>(index);
        if ((mask & ComponentMaskBits.CDropItemMotion) != 0) RemoveComponentAtSlot<CDropItemMotion>(index);
        if ((mask & ComponentMaskBits.CDropItem) != 0) RemoveComponentAtSlot<CDropItem>(index);
        if ((mask & ComponentMaskBits.CEnemyDeathLoot) != 0) RemoveComponentAtSlot<CEnemyDeathLoot>(index);
        if ((mask & ComponentMaskBits.CEnemyPathMovement) != 0) RemoveComponentAtSlot<CEnemyPathMovement>(index);
        if ((mask & ComponentMaskBits.CEnemy) != 0) RemoveComponentAtSlot<CEnemy>(index);
        if ((mask & ComponentMaskBits.CPlayer) != 0) RemoveComponentAtSlot<CPlayer>(index);
        if ((mask & ComponentMaskBits.CHealth) != 0) RemoveComponentAtSlot<CHealth>(index);
        if ((mask & ComponentMaskBits.CCollider) != 0) RemoveComponentAtSlot<CCollider>(index);
        if ((mask & ComponentMaskBits.CVelocity) != 0) RemoveComponentAtSlot<CVelocity>(index);
        if ((mask & ComponentMaskBits.CRotation) != 0) RemoveComponentAtSlot<CRotation>(index);
        if ((mask & ComponentMaskBits.CPosition) != 0) RemoveComponentAtSlot<CPosition>(index);
        if ((mask & ComponentMaskBits.CStageState) != 0) RemoveComponentAtSlot<CStageState>(index);
        if ((mask & ComponentMaskBits.CDanmakuBezierHoming) != 0) RemoveComponentAtSlot<CDanmakuBezierHoming>(index);
        if ((mask & ComponentMaskBits.CMidBossEncounter) != 0) RemoveComponentAtSlot<CMidBossEncounter>(index);

        _componentMasks[index] = 0;
    }

    void RemoveComponentAtSlot<T>(int index) where T : struct, IComponent
    {
        uint bit = ComponentMaskBits.GetMask<T>();
        if (bit == 0)
            return;

        if (ComponentStorage<T>.HasComponent.Get(index))
            ComponentStorage<T>.Remove(index);

        _componentMasks[index] &= ~bit;
    }
}
