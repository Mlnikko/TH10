using UnityEngine;

[CreateAssetMenu(fileName = "NewBossPhaseConfig", menuName = "Configs/Stage/Boss Phase Config")]
public class BossPhaseConfig : GameConfig
{
    public string phaseName;

    [Tooltip("进入该阶段的触发条件: 0=时间到达, 1=血量低于阈值")]
    public TriggerType triggerType;
    public int triggerFrameOffset; // 如果是时间触发
    public float triggerHpPercent; // 如果是血量触发 (0.0 - 1.0)

    [Tooltip("该阶段使用的符卡/攻击模式配置ID (关联另一个SO或枚举)")]
    public string spellCardId;

    [Tooltip("该阶段持续时间(帧), -1表示直到血量归零")]
    public int durationFrames = -1;

    [Tooltip("该阶段特有的背景BGM或特效")]
    public string bgmRef;

    public enum TriggerType { Time, HealthPercent, KillCount }
}
