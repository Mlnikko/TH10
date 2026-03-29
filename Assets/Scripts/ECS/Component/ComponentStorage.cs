using System;
using System.Runtime.CompilerServices;

/// <summary>
/// 组件容器：为每种组件类型维护独立的存储和状态位图。
/// </summary>
/// <typeparam name="T">组件</typeparam>
internal static class ComponentStorage<T> where T : struct, IComponent
{
    public static readonly T[] Components = new T[EntityManager.MAX_ENTITIES];
    public static readonly BitSet HasComponent = new(EntityManager.MAX_ENTITIES);

    public static readonly int[] ActiveIndices = new int[EntityManager.MAX_ENTITIES];
    public static readonly int[] IndexInActiveList = new int[EntityManager.MAX_ENTITIES];
    public static int ActiveCount = 0;

    static ComponentStorage()
    {
        // 初始化为 -1，表示不在列表中
        for (int i = 0; i < IndexInActiveList.Length; i++)
        {
            IndexInActiveList[i] = -1;
        }
    }

    public static void Add(int index, in T component)
    {
        if ((uint)index >= EntityManager.MAX_ENTITIES) return;

        // 如果已存在，仅更新数据
        if (HasComponent.Get(index))
        {
            Components[index] = component;
            return;
        }

        // 1. 设置 BitSet
        HasComponent.Set(index, true);
        Components[index] = component;

        // 2. 加入紧凑列表 (尾部追加)
        IndexInActiveList[index] = ActiveCount;
        ActiveIndices[ActiveCount] = index;
        ActiveCount++;
    }

    public static void Remove(int index)
    {
        if ((uint)index >= EntityManager.MAX_ENTITIES || !HasComponent.Get(index))
            return;

        // 1. 清除 BitSet
        HasComponent.Set(index, false);
        Components[index] = default;

        // 2. 从紧凑列表中移除 (Swap-Remove 技巧)
        int posInList = IndexInActiveList[index];
        int lastIndex = ActiveCount - 1;

        if (posInList != lastIndex)
        {
            // 将最后一个元素移到当前位置
            int lastEntityIndex = ActiveIndices[lastIndex];
            ActiveIndices[posInList] = lastEntityIndex;

            // 更新被移动元素的反向映射
            IndexInActiveList[lastEntityIndex] = posInList;
        }

        // 清理原位置映射
        IndexInActiveList[index] = -1;
        ActiveCount--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<int> GetActiveIndices()
    {
        return new Span<int>(ActiveIndices, 0, ActiveCount);
    }
}
