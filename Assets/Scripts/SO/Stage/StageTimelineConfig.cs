using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStageTimeline", menuName = "Configs/Stage/Stage Timeline Config")]
public class StageTimelineConfig : GameConfig, ILogicTimingBake
{
    [Header("全局设置")]
    public string stageId;

    [Tooltip("关卡 BGM 等资源引用（玩法系统可后续接入）")]
    public string stageBgmRef;

    [Header("道中波次 (Mid-Stage Waves)")]
    [Tooltip("按烘焙后的 startFrameOffset 排序；波次内填秒，在 BakeLogicTiming 中换算为逻辑帧")]
    public List<EnemyWaveConfig> midStageWaves = new();

    [Header("中场 Boss（独立配置文件）")]
    [Tooltip("可为空；启用与否以资产内 enabled 为准")]
    public MidBossEncounterConfig midBossEncounter;

    [Header("关底 Boss（独立配置文件）")]
    [Tooltip("可为空；无配置或 disabled 时跳过关底 Boss 流程")]
    public MainBossEncounterConfig mainBossEncounter;

    [Header("结束条件")]
    [Tooltip("关卡最长持续时间（秒）；≤0 表示不启用超时；烘焙为 maxStageLogicFrames")]
    public float maxStageDurationSeconds = 240f;

    [NonSerialized] public int maxStageLogicFrames;

    public string clearEffectPrefab;

    public void BakeLogicTiming(uint logicFPS)
    {
        if (maxStageDurationSeconds <= 0f)
            maxStageLogicFrames = 0;
        else
        {
            int f = Mathf.RoundToInt(maxStageDurationSeconds * logicFPS);
            maxStageLogicFrames = f < 1 ? 1 : f;
        }

        if (midStageWaves != null)
        {
            for (int i = 0; i < midStageWaves.Count; i++)
            {
                if (midStageWaves[i] is ILogicTimingBake b)
                    b.BakeLogicTiming(logicFPS);
            }
        }

        if (midBossEncounter is ILogicTimingBake midBake)
            midBake.BakeLogicTiming(logicFPS);
        if (mainBossEncounter is ILogicTimingBake mainBake)
            mainBake.BakeLogicTiming(logicFPS);
    }
}
