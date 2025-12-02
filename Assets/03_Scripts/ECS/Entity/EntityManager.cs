using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 位集，用于高效存储和操作大量布尔值状态。
/// </summary>
public class BitSet
{
    private readonly uint[] _data;
    private readonly int _length;

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
    private static int TrailingZeroCount(uint x)
    {
        if (x == 0) return 32;
        int count = 0;
        while ((x & 1) == 0) { count++; x >>= 1; }
        return count;
    }
}

/// <summary>
/// 组件容器：为每种组件类型维护独立的存储和状态位图。
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class ComponentStorage<T> where T : struct, IComponent
{
    public static readonly T[] Array = new T[EntityManager.MAX_ENTITIES];
    public static readonly BitSet HasComponent = new BitSet(EntityManager.MAX_ENTITIES);
    public static readonly BitSet ActiveComponent = new BitSet(EntityManager.MAX_ENTITIES);
}

public class EntityManager
{
    public const int MAX_ENTITIES = 65536; // 必须 ≤ 65536

    private readonly bool[] _activeEntities = new bool[MAX_ENTITIES];
    private readonly ushort[] _versions = new ushort[MAX_ENTITIES]; // 关键：版本数组
    private readonly Queue<int> _freeIds = new Queue<int>();

    public bool[] ActiveEntities => _activeEntities;

    public EntityManager()
    {
        for (int i = 0; i < MAX_ENTITIES; i++)
            _freeIds.Enqueue(i);
    }

    public Entity CreateEntity()
    {
        if (_freeIds.Count == 0)
            throw new InvalidOperationException("Entity pool exhausted!");

        int index = _freeIds.Dequeue();

        // 关键：销毁时已递增过 Version，这里直接使用当前值
        // 但为确保唯一性，创建时再递增一次（可选，保守做法）
        _versions[index]++;
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
        int index = entity.Index;
        return index < MAX_ENTITIES &&
               _activeEntities[index] &&
               _versions[index] == entity.Version;
    }

    // 根据索引获取实体（用于系统内部）
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

        ComponentStorage<T>.Array[i] = component;
        ComponentStorage<T>.HasComponent.Set(i, true);

        // 如果 T 有 Active 字段，可特殊处理（见下文）
    }

    // 泛型移除组件
    public void RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsValid(entity)) return;
        int i = entity.Index;
        ComponentStorage<T>.Array[i] = default;
        ComponentStorage<T>.HasComponent.Set(i, false);
    }

    /// <summary>
    /// 泛型获取组件（引用，可读写）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsValid(entity))
            throw new ArgumentException("Invalid entity");
        return ref ComponentStorage<T>.Array[entity.Index];
    }

    /// <summary>
    /// 获取整个组件数组的 Span（用于系统遍历）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Span<T> GetComponentSpan<T>() where T : struct, IComponent
    {
        return ComponentStorage<T>.Array.AsSpan();
    }

    /// <summary>
    /// 获取活跃组件索引（通用）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="output"></param>
    /// <returns></returns>
    public int GetActiveEntities<T>(Span<int> output) where T : struct, IComponent
    {
        // 默认：只要存在就算活跃
        return ComponentStorage<T>.HasComponent.GetSetBits(output);
    }
}