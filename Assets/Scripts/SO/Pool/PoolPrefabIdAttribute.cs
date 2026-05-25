using UnityEngine;

/// <summary>
/// Inspector 中将 string 绘制为对象池预制体 Id 下拉（按所在 <see cref="PoolCategoryGroup.category"/> 过滤，见 Editor PropertyDrawer）。
/// </summary>
public sealed class PoolPrefabIdAttribute : PropertyAttribute
{
    public E_PoolCategory Category { get; }
    public bool HasExplicitCategory { get; }

    public PoolPrefabIdAttribute()
    {
    }

    public PoolPrefabIdAttribute(E_PoolCategory category)
    {
        Category = category;
        HasExplicitCategory = true;
    }
}
