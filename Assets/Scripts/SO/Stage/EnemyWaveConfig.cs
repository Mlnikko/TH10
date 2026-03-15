using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyWave", menuName = "Configs/Stage/EnemyWaveConfig")]
public class EnemyWaveConfig : GameConfig
{
    [Tooltip("相对于关卡开始或上一节点结束的帧数偏移")]
    public int startFrameOffset;

    [Tooltip("敌人配置引用")]
    public string enemyConfigId;

    [Tooltip("生成数量")]
    public int count = 1;

    [Tooltip("生成阵型 (Grid, Circle, Line, Random)")]
    public SpawnPattern spawnPattern = SpawnPattern.Line;
    public Vector2 spawnAreaSize = new(10, 5); // 生成区域大小
    public Vector2 spawnOffset = Vector2.zero; // 相对屏幕中心的偏移

    [Tooltip("关联的运动行为数据")]
    public MovementPatternData movementData;

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