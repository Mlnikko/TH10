#if UNITY_EDITOR
using UnityEngine;

/// <summary>中场 / 关底 Boss 路径阶段解析（与 <see cref="StageTimelineWaveGizmo"/> 预览一致）。</summary>
public static class StageTimelineBossPathEdit
{
    public const int MidBossPhaseCount = 3;
    public const int MainBossPhaseCount = 2;

    public static string GetMidBossPhaseLabel(int phaseIndex) => phaseIndex switch
    {
        0 => "入场",
        1 => "循环",
        2 => "退场",
        _ => $"阶段 {phaseIndex + 1}"
    };

    public static string GetMainBossPhaseLabel(int phaseIndex) => phaseIndex switch
    {
        0 => "登场",
        1 => "场内",
        _ => $"阶段 {phaseIndex + 1}"
    };

    public static PathRouteMovementData GetMidBossRoute(MidBossEncounterConfig encounter, int phaseIndex) =>
        encounter == null ? null : phaseIndex switch
        {
            0 => encounter.entryPathRoute,
            1 => encounter.loopPathRoute,
            2 => encounter.exitPathRoute,
            _ => null
        };

    public static PathRouteMovementData GetMainBossRoute(MainBossEncounterConfig encounter, int phaseIndex) =>
        encounter == null ? null : phaseIndex switch
        {
            0 => encounter.entryPathRoute,
            1 => encounter.loopPathRoute,
            _ => null
        };

    public static void EnsureMidBossRouteInitialized(MidBossEncounterConfig encounter, int phaseIndex)
    {
        if (encounter == null)
            return;

        var existing = GetMidBossRoute(encounter, phaseIndex);
        if (PathRouteMovementData.HasEditablePathNodes(existing))
        {
            existing.EnsureLegsMatchNodeCount();
            return;
        }

        var route = PathRouteMovementData.CreateLinearDown(32f, 2.4f);
        route.EnsureLegsMatchNodeCount();
        switch (phaseIndex)
        {
            case 0: encounter.entryPathRoute = route; break;
            case 1: encounter.loopPathRoute = route; break;
            case 2: encounter.exitPathRoute = route; break;
        }
    }

    public static void EnsureMainBossRouteInitialized(MainBossEncounterConfig encounter, int phaseIndex)
    {
        if (encounter == null)
            return;

        var existing = GetMainBossRoute(encounter, phaseIndex);
        if (PathRouteMovementData.HasEditablePathNodes(existing))
        {
            existing.EnsureLegsMatchNodeCount();
            return;
        }

        var route = PathRouteMovementData.CreateLinearDown(phaseIndex == 0 ? 24f : 16f, 2f);
        route.EnsureLegsMatchNodeCount();
        if (phaseIndex == 0)
            encounter.entryPathRoute = route;
        else if (phaseIndex == 1)
            encounter.loopPathRoute = route;
    }

    public static Vector2 ResolveMidBossPhaseOrigin(
        MidBossEncounterConfig encounter,
        int phaseIndex,
        in BattleAreaData area,
        uint logicFps)
    {
        Vector2 origin = StageTimelineWaveGizmo.ResolveMidBossSpawn(encounter, area);
        if (phaseIndex <= 0 || encounter == null)
            return origin;

        if (phaseIndex >= 1)
            origin = EvaluateRouteEndIfAny(encounter.entryPathRoute, origin, logicFps, loopSample: false);

        return origin;
    }

    public static Vector2 ResolveMainBossPhaseOrigin(
        MainBossEncounterConfig encounter,
        int phaseIndex,
        in BattleAreaData area,
        uint logicFps)
    {
        Vector2 origin = StageTimelineWaveGizmo.ResolveMainBossSpawn(encounter, area);
        if (phaseIndex <= 0 || encounter == null)
            return origin;

        return EvaluateRouteEndIfAny(encounter.entryPathRoute, origin, logicFps, loopSample: false);
    }

    static Vector2 EvaluateRouteEndIfAny(
        PathRouteMovementData route,
        Vector2 origin,
        uint logicFps,
        bool loopSample)
    {
        if (!PathRouteMovementData.HasAnyPathContent(route))
            return origin;

        return StageTimelineWaveGizmo.EvaluateRouteEndWorld(route, origin, logicFps, loopSample);
    }
}
#endif
