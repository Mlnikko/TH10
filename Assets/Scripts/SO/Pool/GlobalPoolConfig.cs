using System;
using UnityEngine;

public enum E_PoolCategory
{
    Player,          // 角色预制体
    Enemy,           // 敌人预制体
    Danmaku,         // 弹幕预制体
    Drop,            // 掉落物预制体
    Effect,          // 特效预制体
    Weapon,          // 武器预制体（挂于角色下）
    DanmakuEmitter,  // 弹幕发射器预制体（武器布局/表现）
    Other,           // 其他
    Stage,           // 关卡表现（背景云雾等）
}

[Serializable]
public class GlobalPoolEntry
{
    [PoolPrefabId]
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

#if UNITY_EDITOR
    void OnValidate()
    {
        if (poolCategories == null)
            return;

        for (int i = 0; i < poolCategories.Length; i++)
        {
            var entries = poolCategories[i].entries;
            if (entries == null)
                continue;

            for (int j = 0; j < entries.Length; j++)
            {
                if (!string.IsNullOrEmpty(entries[j].prefabId))
                    entries[j].prefabId = StringHelper.NormalizeResourceId(entries[j].prefabId);
            }
        }
    }
#endif
}
