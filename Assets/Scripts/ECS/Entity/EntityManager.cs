using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 位集，用于高效存储和操作大量布尔值状态。
/// </summary>
public class BitSet
{
    static readonly byte[] _tzLookup = new byte[32]
    {
        0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
        31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
    };

    readonly uint[] _data;
    readonly int _length;

    public BitSet(int length)
    {
        _length = length;
        int wordCount = (length + 31) / 32;
        _data = new uint[wordCount];
    }

    public void Set(int index, bool value)
    {
        if ((uint)index >= (uint)_length) return;
        int word = index >> 5;
        int bit = index & 31;
        if (value)
            _data[word] |= (1u << bit);
        else
            _data[word] &= ~(1u << bit);
    }

    public bool Get(int index)
    {
        if ((uint)index >= (uint)_length) return false;
        int word = index >> 5;
        int bit = index & 31;
        return (_data[word] & (1u << bit)) != 0;
    }

    /// <summary>
    /// 清空所有位（设为 false）
    /// </summary>
    public void ClearAll()
    {
        Array.Clear(_data, 0, _data.Length); // 高效清零，无 GC
    }

    /// <summary>
    /// 高效写入所有置位索引到 output，返回数量
    /// </summary>
    /// <param name="output"></param>
    /// <returns></returns>
    public int GetSetBits(Span<int> output)
    {
        int written = 0;
        int maxWords = (_length + 31) / 32;

        for (int wordIndex = 0; wordIndex < maxWords; wordIndex++)
        {
            uint word = _data[wordIndex];
            if (word == 0) continue;

            int baseIndex = wordIndex * 32;
            while (word != 0 && written < output.Length)
            {
                // 获取最低位 1 的位置
                int tz = TrailingZeroCount(word);
                int index = baseIndex + tz;
                if (index >= _length) break;

                output[written++] = index;
                word &= word - 1; // 清除最低位
            }
            if (written >= output.Length) break;
        }
        return written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int TrailingZeroCount(uint x)
    {
        if (x == 0) return 32;
        // De Bruijn 算法：O(1)，无循环
        return _tzLookup[((uint)((x & -(int)x) * 0x077CB531U)) >> 27];
    }
}

/// <summary>
/// 预分配临时位集：用于系统内部临时计算，避免频繁分配。
/// </summary>
public static class TempBitSets
{
    public static readonly BitSet Collision = new BitSet(EntityManager.MAX_ENTITIES);
}

/// <summary>
/// 组件容器：为每种组件类型维护独立的存储和状态位图。
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class ComponentStorage<T> where T : struct, IComponent
{
    public static readonly T[] Components = new T[EntityManager.MAX_ENTITIES];
    public static readonly BitSet HasComponent = new BitSet(EntityManager.MAX_ENTITIES);
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
    public const int MAX_ENTITIES = 65536; // 必须 ≤ 65536

    readonly bool[] _activeEntities = new bool[MAX_ENTITIES];
    readonly ushort[] _versions = new ushort[MAX_ENTITIES]; // 版本数组
    readonly Queue<int> _freeIds = new();

    public bool[] ActiveEntities => _activeEntities;

    public EntityManager()
    {
        Initialize();
    }

    void Initialize()
    {
        for (int i = 0; i < MAX_ENTITIES; i++)
        {
            _freeIds.Enqueue(i);
            _versions[i] = 1; // ← 确保首个实体 Version ≥ 1
            _activeEntities[i] = false;
        }
    }

    public Entity CreateEntity()
    {
        if (_freeIds.Count == 0)
            throw new InvalidOperationException("Entity pool exhausted!");

        int index = _freeIds.Dequeue();

        _activeEntities[index] = true;

        Entity entity = Entity.FromIndexAndVersion(index, _versions[index]);

        return entity;
    }
    public void DestroyEntity(Entity entity)
    {
        if (entity.IsNull) return;

        int index = entity.Index;
        ushort version = entity.Version;

        // 安全校验：防止重复销毁或销毁无效句柄
        if (index >= MAX_ENTITIES || !_activeEntities[index] || _versions[index] != version)
            return;

        // 销毁时递增 Version！这是防复用的关键
        _versions[index]++;
        _activeEntities[index] = false;
        _freeIds.Enqueue(index);
    }

    // 核心安全方法：检查 Entity 是否有效
    public bool IsValid(Entity entity)
    {
        if (entity.IsNull) return false;
        return entity.Index < MAX_ENTITIES && _activeEntities[entity.Index] && _versions[entity.Index] == entity.Version;
    }
    public bool IsValid(int index)
    {
        Entity entity = GetEntityByIndex(index);
        return IsValid(entity);
    }

    // 根据索引获取实体
    public Entity GetEntityByIndex(int index)
    {
        if (index < 0 || index >= MAX_ENTITIES || !_activeEntities[index])
            return Entity.Null;
        return Entity.FromIndexAndVersion(index, _versions[index]);
    }

    // 泛型添加组件
    public void AddComponent<T>(Entity entity, in T component) where T : struct, IComponent
    {
        if (!IsValid(entity)) return;
        int i = entity.Index;

        ComponentStorage<T>.Components[i] = component;
        ComponentStorage<T>.HasComponent.Set(i, true);
    }

    // 泛型移除组件
    public void RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsValid(entity)) return;
        int i = entity.Index;
        ComponentStorage<T>.Components[i] = default;
        ComponentStorage<T>.HasComponent.Set(i, false);
    }

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

    public int GetEntities<T>(Span<int> output) where T : struct, IComponent
    {
        return ComponentStorage<T>.HasComponent.GetSetBits(output);
    }
}