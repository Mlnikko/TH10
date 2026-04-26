using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关底 Boss 单独配置资产（登场时机、符卡阶段列表等）。由 <see cref="StageTimelineConfig"/> 引用。
/// </summary>
[CreateAssetMenu(fileName = "MainBossEncounter", menuName = "Configs/Stage/Main Boss Encounter")]
public class MainBossEncounterConfig : GameConfig, ILogicTimingBake
{
    [Tooltip("是否启用关底 Boss")]
    public bool enabled = true;

    [Tooltip("相对关卡开始的登场时刻（秒）；在 BakeLogicTiming 中烘焙为 spawnFrameOffset")]
    public float spawnTimeSeconds = 120f;

    [NonSerialized] public int spawnFrameOffset;

    [Tooltip("敌人配置 id（EnemyConfig 的 ConfigId）")]
    public string enemyConfigId;

    [Tooltip("相对战斗区中心的位移")]
    public Vector2 spawnOffset;

    [Tooltip("相对战斗区高度的 Y 归一化偏移")]
    [Range(-0.5f, 0.5f)]
    public float yHeightNorm = 0.2f;

    [Tooltip("BOSS 登场后的对话/无敌时间（秒）；加载时烘焙为 bossIntroDurationFrames")]
    public float bossIntroDurationSeconds = 3f;

    [NonSerialized] public int bossIntroDurationFrames;

    [Tooltip("BOSS 阶段 / 符卡（独立 BossPhase 资产）")]
    public List<BossPhaseConfig> bossPhases = new();

    public void BakeLogicTiming(uint logicFPS)
    {
        spawnFrameOffset = spawnTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(spawnTimeSeconds * logicFPS));
        bossIntroDurationFrames = bossIntroDurationSeconds <= 0f
            ? 0
            : Mathf.Max(0, Mathf.RoundToInt(bossIntroDurationSeconds * logicFPS));
        if (bossPhases == null)
            return;
        for (int i = 0; i < bossPhases.Count; i++)
        {
            if (bossPhases[i] is ILogicTimingBake phaseBake)
                phaseBake.BakeLogicTiming(logicFPS);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        enemyConfigId = enemyConfigId.ToLowerInvariantTrimmed();
    }
#endif
}
