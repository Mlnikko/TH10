using System;
using UnityEngine;

/// <summary>
/// 中场 Boss 单独配置资产。由 <see cref="StageTimelineConfig"/> 引用。
/// </summary>
[CreateAssetMenu(fileName = "MidBossEncounter", menuName = "Configs/Stage/Mid Boss Encounter")]
public class MidBossEncounterConfig : GameConfig, ILogicTimingBake
{
    [Tooltip("是否启用本场中场 Boss")]
    public bool enabled = true;

    [Tooltip("相对关卡开始的登场时刻（秒）；在 BakeLogicTiming 中烘焙为 spawnFrameOffset")]
    public float spawnTimeSeconds = 50f;

    [NonSerialized] public int spawnFrameOffset;

    public void BakeLogicTiming(uint logicFPS)
    {
        spawnFrameOffset = spawnTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(spawnTimeSeconds * logicFPS));
        introMovement?.BakeMovementTiming(logicFPS);
    }

    [Tooltip("敌人配置 id（EnemyConfig 的 ConfigId）")]
    public string enemyConfigId;

    [Tooltip("相对战斗区中心的位移")]
    public Vector2 spawnOffset;

    [Tooltip("相对战斗区高度的 Y 归一化偏移（在 spawnOffset 基础上叠加 area.Height * yHeightNorm）")]
    [Range(-0.5f, 0.5f)]
    public float yHeightNorm = 0.25f;

    [SerializeReference]
    [MovementPatternSerialize]
    public MovementPatternData introMovement;

#if UNITY_EDITOR
    void OnValidate()
    {
        enemyConfigId = enemyConfigId.ToLowerInvariantTrimmed();
    }
#endif
}
