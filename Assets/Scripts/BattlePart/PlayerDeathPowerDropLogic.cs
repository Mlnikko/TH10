using System;

/// <summary>
/// 玩家死亡 Power 散落：按扣除后数值优先大 P、余数用小 P 补，从中心径向散开。
/// </summary>
public static class PlayerDeathPowerDropLogic
{
    public const int MaxDropCount = 24;

    public readonly struct DropSpawnPlan
    {
        public readonly int cfgIndex;
        public readonly float burstDirX;
        public readonly float burstDirY;

        public DropSpawnPlan(int cfgIndex, float burstDirX, float burstDirY)
        {
            this.cfgIndex = cfgIndex;
            this.burstDirX = burstDirX;
            this.burstDirY = burstDirY;
        }
    }

    /// <summary>
    /// 按大 P / 小 P 面值拆分扣除后的 Power；余数不足一小 P 的部分丢弃。
    /// </summary>
    public static void ComputeDropCounts(
        int powerOrbs,
        int deathPowerDeduction,
        int largePowerValue,
        int smallPowerValue,
        out int largeCount,
        out int smallCount)
    {
        largeCount = 0;
        smallCount = 0;

        if (powerOrbs <= 0 || largePowerValue <= 0 || smallPowerValue <= 0)
            return;

        int dropPower = Math.Max(0, powerOrbs - Math.Max(0, deathPowerDeduction));
        if (dropPower <= 0)
            return;

        largeCount = dropPower / largePowerValue;
        int remainder = dropPower % largePowerValue;
        smallCount = remainder / smallPowerValue;
    }

    public static int BuildSpawnPlans(
        CharacterConfig characterCfg,
        int powerOrbs,
        Span<DropSpawnPlan> plans)
    {
        if (characterCfg == null || plans.Length == 0 || powerOrbs <= 0)
            return 0;

        if (characterCfg.deathPowerLargeDropCfgIndex < 0 && characterCfg.deathPowerSmallDropCfgIndex < 0)
            return 0;

        var largeCfg = characterCfg.deathPowerLargeDropCfgIndex >= 0
            ? GameResDB.Instance.GetConfig<DropItemConfig>(characterCfg.deathPowerLargeDropCfgIndex)
            : null;
        var smallCfg = characterCfg.deathPowerSmallDropCfgIndex >= 0
            ? GameResDB.Instance.GetConfig<DropItemConfig>(characterCfg.deathPowerSmallDropCfgIndex)
            : null;

        bool hasLarge = largeCfg != null;
        bool hasSmall = smallCfg != null;
        if (!hasLarge && !hasSmall)
            return 0;

        int largeValue = largeCfg != null && largeCfg.dropKind == E_DropKind.Power
            ? Math.Max(1, largeCfg.effectAmount)
            : 50;
        int smallValue = smallCfg != null && smallCfg.dropKind == E_DropKind.Power
            ? Math.Max(1, smallCfg.effectAmount)
            : 10;

        ComputeDropCounts(
            powerOrbs,
            characterCfg.deathPowerDeduction,
            largeValue,
            smallValue,
            out int largeCount,
            out int smallCount);

        if (!hasLarge)
            largeCount = 0;
        if (!hasSmall)
            smallCount = 0;

        int total = largeCount + smallCount;
        if (total <= 0)
            return 0;

        if (total > MaxDropCount)
        {
            int overflow = total - MaxDropCount;
            int trimSmall = Math.Min(overflow, smallCount);
            smallCount -= trimSmall;
            overflow -= trimSmall;
            largeCount = Math.Max(0, largeCount - overflow);
            total = largeCount + smallCount;
        }

        if (total <= 0)
            return 0;

        int written = 0;
        const float twoPi = MathF.PI * 2f;

        for (int i = 0; i < largeCount && written < plans.Length; i++)
        {
            ResolveRadialDirection(written, total, twoPi, out float dirX, out float dirY);
            plans[written++] = new DropSpawnPlan(characterCfg.deathPowerLargeDropCfgIndex, dirX, dirY);
        }

        for (int i = 0; i < smallCount && written < plans.Length; i++)
        {
            ResolveRadialDirection(written, total, twoPi, out float dirX, out float dirY);
            plans[written++] = new DropSpawnPlan(characterCfg.deathPowerSmallDropCfgIndex, dirX, dirY);
        }

        return written;
    }

    static void ResolveRadialDirection(int index, int total, float twoPi, out float dirX, out float dirY)
    {
        float angle = total > 0 ? twoPi * index / total : 0f;
        dirX = MathF.Cos(angle);
        dirY = MathF.Sin(angle);
    }

    public static void SpawnAt(
        EntityManager em,
        EntityFactory factory,
        float centerX,
        float centerY,
        ReadOnlySpan<DropSpawnPlan> plans)
    {
        for (int i = 0; i < plans.Length; i++)
        {
            ref readonly var plan = ref plans[i];
            if (plan.cfgIndex < 0)
                continue;

            Entity drop = factory.CreateDropItem(
                plan.cfgIndex, centerX, centerY, plan.burstDirX, plan.burstDirY, true);
            if (!drop.IsNull)
                em.AddComponent(drop, new CPoolGetTag());
        }
    }
}
