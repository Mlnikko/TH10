using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewBossPhaseConfig", menuName = "Configs/Stage/Boss Phase Config")]
public class BossPhaseConfig : GameConfig, IReferenceResolver, ILogicTimingBake
{
    public string phaseName;

    [Tooltip("进入该阶段的触发条件: 0=时间到达, 1=血量低于阈值")]
    public TriggerType triggerType;

    [Tooltip("自 BossFight 起经过多少秒后进入该阶段（时间触发时）；加载时烘焙为 triggerFrameOffset")]
    public float triggerTimeSeconds;

    [NonSerialized] public int triggerFrameOffset;

    public float triggerHpPercent; // 如果是血量触发 (0.0 - 1.0)

    [Tooltip("该阶段使用的弹幕发射器 ConfigId（DanmakuEmitterConfig）")]
    public string spellCardId;

    [NonSerialized] public int spellCardEmitterIndex = -1;

    [Tooltip("该阶段持续时间（秒）；<0 表示直到血量归零（烘焙为 durationFrames = -1）")]
    public float durationSeconds = -1f;

    [NonSerialized] public int durationFrames = -1;

    [Tooltip("该阶段特有的背景BGM或特效")]
    public string bgmRef;

    public enum TriggerType { Time, HealthPercent, KillCount }

    public void ResolveReferences(GameResDB resDb)
    {
        string id = StringHelper.NormalizeResourceId(spellCardId);
        spellCardEmitterIndex = string.IsNullOrEmpty(id) ? -1 : resDb.GetConfigIndex(id);
        if (!string.IsNullOrEmpty(id) && spellCardEmitterIndex < 0)
        {
            Logger.Warn(
                $"[BossPhaseConfig] DanmakuEmitterConfig not found: '{id}' (phase: {ConfigId})",
                LogTag.Resource);
        }
    }

    public void BakeLogicTiming(uint logicFPS)
    {
        triggerFrameOffset = triggerTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(triggerTimeSeconds * logicFPS));
        if (durationSeconds < 0f)
            durationFrames = -1;
        else
            durationFrames = Mathf.Max(0, Mathf.RoundToInt(durationSeconds * logicFPS));
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        spellCardId = StringHelper.NormalizeResourceId(spellCardId);
    }
#endif
}
