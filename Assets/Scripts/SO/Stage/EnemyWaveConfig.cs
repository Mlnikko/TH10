using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyWave", menuName = "Configs/Stage/EnemyWaveConfig")]
public class EnemyWaveConfig : GameConfig, ILogicTimingBake
{
    [Tooltip("相对关卡开始的时刻（秒）；在 ILogicTimingBake.BakeLogicTiming 中烘焙为 startFrameOffset")]
    public float startTimeSeconds;

    [NonSerialized] public int startFrameOffset;

    public void BakeLogicTiming(uint logicFPS)
    {
        startFrameOffset = startTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(startTimeSeconds * logicFPS));
        movementData?.BakeMovementTiming(logicFPS);
    }

    [Tooltip("敌人配置引用")]
    public string enemyConfigId;

    [Tooltip("生成数量")]
    public int count = 1;

    [Tooltip("生成阵型 (Grid, Circle, Line, Random)")]
    public SpawnPattern spawnPattern = SpawnPattern.Line;
    public Vector2 spawnAreaSize = new(10, 5); // 生成区域大小
    public Vector2 spawnOffset = Vector2.zero; // 相对屏幕中心的偏移

    [SerializeReference]
    [MovementPatternSerialize]
    [Tooltip("本波敌人运动轨迹（东方系折线/贝塞尔/正圆等）；为空时可用下方默认下落")]
    public MovementPatternData movementData;

    [Tooltip("未配置 movementData 时是否使用默认竖直下落")]
    public bool useDefaultDescentIfNoMovement = true;

    [Min(0f)]
    [Tooltip("默认下落速度（世界单位 / 逻辑帧），仅当未配置 movementData 且上一项为真时生效")]
    public float defaultDescentSpeedPerFrame = 0.06f;

    [Tooltip("初始血量倍率 (用于难度调整)")]
    public float hpMultiplier = 1.0f;

    [Tooltip("是否等待此波次全灭后才继续后续逻辑 (仅用于特定脚本控制，时间线通常自动推进)")]
    public bool waitForClear = false;

#if UNITY_EDITOR
    void OnValidate()
    {
        enemyConfigId = enemyConfigId.ToLowerInvariantTrimmed();
    }
#endif
}