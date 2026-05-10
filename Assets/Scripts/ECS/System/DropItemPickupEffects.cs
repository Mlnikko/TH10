using System;

/// <summary>
/// 掉落物拾取效果（仅确定性逻辑；不涉及 Unity API）。
/// </summary>
public static class DropItemPickupEffects
{
    public static void Apply(in DropItemConfig cfg, EntityManager em, Entity playerEntity)
    {
        if (!em.IsValid(playerEntity) || cfg == null)
            return;

        int amt = Math.Max(0, cfg.effectAmount);
        switch (cfg.dropKind)
        {
            case E_DropKind.Score:
                GlobalBattleData.AddSessionScore(amt);
                break;

            case E_DropKind.Heal:
                if (em.HasComponent<CHealth>(playerEntity))
                {
                    ref var hp = ref em.GetComponent<CHealth>(playerEntity);
                    hp.currentHealth = Math.Min(hp.maxHealth, hp.currentHealth + amt);
                }
                break;

            case E_DropKind.Power:
                if (em.HasComponent<CPlayer>(playerEntity))
                {
                    ref var pl = ref em.GetComponent<CPlayer>(playerEntity);
                    pl.powerOrbs += amt;
                }
                break;

            case E_DropKind.None:
            default:
                break;
        }
    }
}
