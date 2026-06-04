using UnityEngine;

/// <summary>
/// 关底 Boss 符卡阶段：切换弹幕发射器（<see cref="BossPhaseConfig.spellEmitters"/> / 兼容 <see cref="BossPhaseConfig.spellCardId"/>）。
/// </summary>
public static class MainBossEncounterPhaseLogic
{
    public static void ApplySpellPhase(EntityManager em, Entity bossEntity, BossPhaseConfig phase)
    {
        EnemyOwnedEmitterLogic.ApplySpellEmitters(em, bossEntity, phase);
    }

    public static void ApplySpellPhaseByIndex(
        EntityManager em,
        Entity bossEntity,
        MainBossEncounterConfig encounter,
        int phaseIndex)
    {
        if (encounter?.bossPhases == null || phaseIndex < 0 || phaseIndex >= encounter.bossPhases.Count)
            return;

        ApplySpellPhase(em, bossEntity, encounter.bossPhases[phaseIndex]);
    }

    public static int ResolveActivePhaseIndex(
        MainBossEncounterConfig encounter,
        EntityManager em,
        Entity bossEntity,
        uint fightElapsed)
    {
        if (encounter?.bossPhases == null || encounter.bossPhases.Count == 0)
            return -1;

        int best = -1;
        float hpRatio = 1f;
        if (em.IsValid(bossEntity) && em.HasComponent<CEnemy>(bossEntity))
        {
            ref readonly var enemy = ref em.GetComponent<CEnemy>(bossEntity);
            var cfg = GameResDB.Instance.GetConfig<EnemyConfig>(enemy.enemyCfgIndex);
            if (cfg != null && cfg.maxHealth > 0)
                hpRatio = enemy.currentHealth / (float)Mathf.Max(1, cfg.maxHealth);
        }

        for (int i = 0; i < encounter.bossPhases.Count; i++)
        {
            var phase = encounter.bossPhases[i];
            if (phase == null)
                continue;

            switch (phase.triggerType)
            {
                case BossPhaseConfig.TriggerType.Time:
                    if (fightElapsed >= (uint)phase.triggerFrameOffset)
                        best = i;
                    break;
                case BossPhaseConfig.TriggerType.HealthPercent:
                    if (hpRatio <= phase.triggerHpPercent)
                        best = i;
                    break;
            }
        }

        return best;
    }
}
