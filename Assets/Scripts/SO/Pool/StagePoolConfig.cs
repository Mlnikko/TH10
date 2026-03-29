using System;
using UnityEngine;

[Serializable]
public class StagePoolOverride
{
    public string prefabId;

    [Tooltip("该场景特定的最大上限")]
    public int overrideMaxCapacity;

    [Tooltip("该场景特定的预热数量")]
    public int overrideWarmupCount;
}

[CreateAssetMenu(fileName = "NewStagePoolConfig", menuName = "Configs/Pool/Stage Pool Config")]
public class StagePoolConfig : GameConfig
{
    [Header("仅配置需要调整的项，未列出的项使用全局默认")]
    public StagePoolOverride[] overrides;
}