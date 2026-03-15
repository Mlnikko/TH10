using System;
using UnityEngine;
using System.Collections.Generic;

// 1. 运动模式参数基类 (多态支持不同的运动逻辑)
[Serializable]
public abstract class MovementPatternData
{
    public enum PatternType { Static, Linear, Sine, Circle, Bezier, Aimed, Spiral }
    public PatternType type;

    // 通用参数 (根据type不同，ECS系统会读取不同的字段)
    public float speed = 100f;
    public float amplitude = 50f; // 正弦/圆形幅度
    public float frequency = 1f;  // 频率
    public Vector2 direction = Vector2.down;
    public List<Vector2> bezierPoints = new List<Vector2>(); // 贝塞尔曲线点

    // 构造函数或工厂方法可在Editor扩展中简化填写
}

// 具体实现示例 (实际项目中可以合并到一个类用Enum区分，或者保持多态)
[Serializable]
public class SineMovementData : MovementPatternData
{
    public SineMovementData() { type = PatternType.Sine; }
    // 特有参数可在此扩展
}

public enum SpawnPattern { Line, Grid, Circle, Random, BossCenter }


[CreateAssetMenu(fileName = "NewStageTimeline", menuName = "Configs/Stage/Stage Timeline Config")]
public class StageTimelineConfig : GameConfig
{
    [Header("全局设置")]
    public string stageId; // 唯一标识，如 "Stage1_Normal"
    public int targetFrameRate = 60; // 设计基准帧率
    public string stageBgmRef; // 关卡背景音乐

    [Header("道中波次 (Mid-Stage Waves)")]
    [Tooltip("按 startFrameOffset 排序，系统在运行时会自动排序以防配置错误")]
    public List<EnemyWaveConfig> midStageWaves = new();

    [Header("中BOSS (Half-way Boss)")]
    public bool hasMidBoss = false;
    public int midBossSpawnFrame = 3000; // 例如第3000帧出现
    public string midBossPrefabId;
    public MovementPatternData midBossIntroMove; // 登场动画轨迹

    [Header("主BOSS战 (Main Boss Battle)")]
    public bool hasMainBoss = true;
    public int mainBossSpawnFrame = 7200; // 例如第7200帧 (约2分钟) 出现
    public string mainBossPrefabId;

    [Tooltip("BOSS登场后的对话/无敌时间 (帧)")]
    public int bossIntroDurationFrames = 180; // 3秒 @60fps

    [Tooltip("BOSS的各个阶段/符卡")]
    public List<BossPhaseConfig> bossPhases = new();

    [Header("结束条件")]
    public int maxStageFrames = 14400; // 4分钟，超时强制结束或进入下一关
    public string clearEffectPrefab;
}