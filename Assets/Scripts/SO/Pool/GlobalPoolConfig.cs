using System;
using UnityEngine;

public enum E_PoolCategory
{
    Player,     // 角色
    Enemy,      // 敌人
    Danmaku,    // 弹幕
    Drop,       // 掉落物 (道具/碎片)
    Effect,     // 特效 (可选，如爆炸、激光)
    Other       // 其他
}

[Serializable]
public class GlobalPoolEntry
{
    public string prefabId;

    [Tooltip("全局允许的最大对象池上限")]
    public int maxCapacity;

    [Tooltip("默认开局预热数量")]
    public int defaultWarmupCount;

    [Tooltip("是否允许在该对象池为空时尝试强制回收复用")]
    public bool allowForceRecycle;
}

// 分组配置容器 (可选，如果你需要在 SO Inspector 里折叠显示)
[Serializable]
public class PoolCategoryGroup
{
    [SerializeField] string categoryName;
    public E_PoolCategory category;
    public GlobalPoolEntry[] entries;
}

[CreateAssetMenu(fileName = "NewGlobalPoolConfig", menuName = "Configs/Pool/Global Pool Config")]
public class GlobalPoolConfig : GameConfig
{
    public PoolCategoryGroup[] poolCategories;
}
