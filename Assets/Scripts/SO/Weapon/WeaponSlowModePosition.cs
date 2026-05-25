using UnityEngine;

/// <summary>低速时发射器槽位相对玩家的表现方式（主炮 / 副炮可分别配置）。</summary>
public enum E_WeaponSlowSlotPositionMode
{
    /// <summary>灵梦式：低速时槽位向玩家中心收束（<see cref="WeaponSlowModeLayoutConfig.secondarySlotConverge"/>）。</summary>
    ConvergeToPlayer = 0,

    /// <summary>
    /// 魔理沙 A：通常模式下副炮偏移随移动方向展开；进入低速后冻结当前相对偏移，仅随玩家平移。
    /// </summary>
    TrailFollowWhileFast = 1,

    /// <summary>魔理沙 B：低速时发射器留在进入低速时的世界坐标；退出低速后回到玩家身边。</summary>
    WorldAnchorWhileSlow = 2,
}

/// <summary>根据 <see cref="WeaponSlowModeLayoutConfig"/> 计算主/副炮槽位偏移（逻辑与表现共用）。</summary>
public static class WeaponSlowModePosition
{
    public const byte SlowStateWorldAnchor = 1;

    public static Vector2 ResolvePrimarySlotOffset(
        WeaponConfig weapon,
        bool slowMode,
        int powerOrbs,
        float converge01,
        Vector2 runtimeOffset)
    {
        if (weapon == null)
            return Vector2.zero;

        var layout = weapon.slowModeLayout ?? new WeaponSlowModeLayoutConfig();
        Vector2 baseOffset = weapon.primaryEmitters.normal.slotOffset;
        if (slowMode && weapon.TryResolvePowerPrimarySlow(powerOrbs, out var tier))
            baseOffset = tier.slot.slotOffset;

        return ResolveSlotOffset(
            layout.primarySlowPositionMode,
            baseOffset,
            slowMode,
            converge01,
            runtimeOffset,
            layout.primarySlotConverge);
    }

    public static Vector2 ResolveSecondarySlotOffset(
        WeaponConfig weapon,
        Vector2 baseOffset,
        bool slowMode,
        float converge01,
        Vector2 runtimeOffset)
    {
        if (weapon == null)
            return baseOffset;

        var layout = weapon.slowModeLayout ?? new WeaponSlowModeLayoutConfig();
        return ResolveSlotOffset(
            layout.secondarySlowPositionMode,
            baseOffset,
            slowMode,
            converge01,
            runtimeOffset,
            layout.secondarySlotConverge);
    }

    static Vector2 ResolveSlotOffset(
        E_WeaponSlowSlotPositionMode mode,
        Vector2 baseOffset,
        bool slowMode,
        float converge01,
        Vector2 runtimeOffset,
        float slotConverge)
    {
        switch (mode)
        {
            case E_WeaponSlowSlotPositionMode.TrailFollowWhileFast:
                return runtimeOffset;

            case E_WeaponSlowSlotPositionMode.WorldAnchorWhileSlow:
                return slowMode ? Vector2.zero : runtimeOffset;

            case E_WeaponSlowSlotPositionMode.ConvergeToPlayer:
            default:
                if (!slowMode)
                    return baseOffset;

                float converge = slotConverge * Mathf.Clamp01(converge01);
                return baseOffset * (1f - converge);
        }
    }

    /// <summary>通常模式下根据移速推进「轨迹跟随」运行时偏移（低速时不调用）。</summary>
    public static void StepTrailFollowOffset(
        ref Vector2 runtimeOffset,
        Vector2 configSlot,
        float velX,
        float velY,
        WeaponSlowModeLayoutConfig layout,
        uint logicFps)
    {
        if (layout == null)
            return;

        float fps = Mathf.Max(1f, logicFps);
        Vector2 target = configSlot;
        float speedSq = velX * velX + velY * velY;

        if (speedSq > 1e-8f)
        {
            float speed = Mathf.Sqrt(speedSq);
            Vector2 behind = new Vector2(-velX / speed, -velY / speed);
            float spread = Mathf.Min(
                layout.secondaryTrailMaxOffset,
                speed * layout.secondaryTrailSpreadPerSpeed);
            target = configSlot + behind * spread;
        }

        float step = layout.secondaryTrailCatchUpSpeed / fps;
        runtimeOffset = Vector2.MoveTowards(runtimeOffset, target, step);
    }

    /// <summary>退出低速后，将运行时偏移拉回配置槽位（WorldAnchor / Trail 共用）。</summary>
    public static void StepReturnToConfigSlot(
        ref Vector2 runtimeOffset,
        Vector2 configSlot,
        WeaponSlowModeLayoutConfig layout,
        uint logicFps)
    {
        if (layout == null)
            return;

        float fps = Mathf.Max(1f, logicFps);
        float step = layout.secondaryReturnToSlotSpeed / fps;
        runtimeOffset = Vector2.MoveTowards(runtimeOffset, configSlot, step);
    }
}
