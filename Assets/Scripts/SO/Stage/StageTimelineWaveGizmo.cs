using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 关卡时间轴波次：刷怪点、路径节点、运动采样与 GO 回收退场点（编辑器 Scene 可视化）。
/// </summary>
public static class StageTimelineWaveGizmo
{
    public struct PathNodeVisual
    {
        public Vector2 worldPos;
        public int nodeIndex;
        public float holdSeconds;
        public bool isEnd;
    }

    public struct SpawnPathVisual
    {
        public Vector2 spawn;
        /// <summary>出怪队列条目索引。</summary>
        public int queueEntryIndex;
        /// <summary>阵型槽位索引。</summary>
        public int formationSlotIndex;
        public List<PathNodeVisual> nodes;
        public List<Vector2> sampledPath;
        public Vector2 exit;
        public bool hasExit;
        /// <summary>表现采样路径标题（绘在路径中段，仅高亮条目）。</summary>
        public string routeHeader;
        public bool usesEntryPathOverride;
    }

    const int DefaultMaxPathSamples = 128;
    const int SamplesPerTravelLeg = 24;
    const int SamplesPerHoldLeg = 4;

    /// <summary>编辑器：构建队列中每名敌人的轨迹（不写入 BakeCache）。</summary>
    public static List<SpawnPathVisual> BuildEditorPathVisuals(
        EnemyWaveConfig wave,
        in BattleAreaData area,
        int waveIndex,
        uint logicFps,
        int maxPathSamples = DefaultMaxPathSamples)
    {
        var result = new List<SpawnPathVisual>();
        if (wave == null)
            return result;

        wave.BakeLogicTiming(logicFps);
        int entryCount = wave.ResolveSpawnCount();
        for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            if (!EnemyWaveSpawnMath.TryResolveQueueEntrySpawn(wave, area, waveIndex, 0, entryIndex, out Vector2 spawn))
                continue;

            if (TryBuildEntryPathVisual(
                    wave, entryIndex, spawn, area, waveIndex, 0, logicFps, maxPathSamples, out var visual))
                result.Add(visual);
        }

        return result;
    }

    static bool TryBuildEntryPathVisual(
        EnemyWaveConfig wave,
        int entryIndex,
        Vector2 spawn,
        in BattleAreaData area,
        int waveIndex,
        uint spawnFrame,
        uint logicFps,
        int maxPathSamples,
        out SpawnPathVisual visual)
    {
        visual = default;
        wave.EnsureSpawnQueueMigrated();
        var entry = wave.spawnQueue[entryIndex];
        int slot = entry.spawnSlotIndex >= 0 ? entry.spawnSlotIndex : entryIndex;

        PathRouteMovementData routeForNodes = wave.ResolvePathForEntry(entryIndex);
        PathRouteMovementData routeForBake = wave.ResolveEffectivePathRoute(entryIndex);
        BakedPathRoute bakedRoute = null;
        if (routeForBake != null)
            bakedRoute = EnemyPathMovementBaking.BakeRoute(routeForBake, logicFps);
        else if (wave.useDefaultDescentIfNoMovement)
        {
            routeForNodes = PathRouteMovementData.CreateLinearDown(48f, wave.defaultDescentSpeed);
            bakedRoute = EnemyPathMovementBaking.BakeRoute(routeForNodes, logicFps);
        }

        bool usesOverride = wave.pathAssignment == E_WavePathAssignment.PerQueueEntry
                            && EnemyWaveConfig.HasUsablePathRoute(entry.pathRouteOverride);

        visual = new SpawnPathVisual
        {
            spawn = spawn,
            queueEntryIndex = entryIndex,
            formationSlotIndex = slot,
            nodes = BuildNodeVisuals(routeForNodes, spawn),
            sampledPath = new List<Vector2>(maxPathSamples + 2),
            usesEntryPathOverride = usesOverride
        };

        string entryLabel = $"#{entryIndex + 1}";

        if (bakedRoute == null || bakedRoute.legCount == 0)
        {
            visual.sampledPath.Add(spawn);
            PopulateRouteAnnotations(ref visual, routeForNodes, logicFps, entryLabel, null);
            return true;
        }

        FillSampledPathFromBakedRoute(bakedRoute, spawn, visual.sampledPath, maxPathSamples, loopRoute: false);

        FinalizeSpawnPathExit(area, ref visual);
        PopulateRouteAnnotations(ref visual, routeForBake ?? routeForNodes, logicFps, entryLabel, bakedRoute);
        return true;
    }

    public static List<SpawnPathVisual> BuildPlayPathVisuals(
        EnemyWaveConfig wave,
        in BattleAreaData area,
        int waveIndex,
        uint logicFps,
        int maxPathSamples = DefaultMaxPathSamples)
    {
        var result = new List<SpawnPathVisual>();
        if (wave == null)
            return result;

        wave.BakeLogicTiming(logicFps);
        wave.BakePathRouteIfNeeded(logicFps);

        uint spawnFrame = 0;
        int entryCount = wave.ResolveSpawnCount();
        for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            if (!EnemyWaveSpawnMath.TryResolveQueueEntrySpawn(wave, area, waveIndex, spawnFrame, entryIndex, out Vector2 spawn))
                continue;

            wave.EnsureSpawnQueueMigrated();
            var entry = wave.spawnQueue[entryIndex];
            int slot = entry.spawnSlotIndex >= 0 ? entry.spawnSlotIndex : entryIndex;
            bool usesOverride = wave.pathAssignment == E_WavePathAssignment.PerQueueEntry
                                && EnemyWaveConfig.HasUsablePathRoute(entry.pathRouteOverride);
            string entryLabel = $"#{entryIndex + 1}";

            var visual = new SpawnPathVisual
            {
                spawn = spawn,
                queueEntryIndex = entryIndex,
                formationSlotIndex = slot,
                nodes = BuildNodeVisuals(wave.ResolvePathForEntry(entryIndex), spawn),
                sampledPath = new List<Vector2>(maxPathSamples + 2),
                usesEntryPathOverride = usesOverride
            };

            PathRouteMovementData routeForNodes = wave.ResolvePathForEntry(entryIndex);
            PathRouteMovementData routeForBake = wave.ResolveEffectivePathRoute(entryIndex);
            BakedPathRoute bakedRoute = null;
            if (routeForBake != null)
                bakedRoute = EnemyPathMovementBaking.BakeRoute(routeForBake, logicFps);
            else if (wave.useDefaultDescentIfNoMovement)
            {
                routeForNodes = PathRouteMovementData.CreateLinearDown(48f, wave.defaultDescentSpeed);
                routeForBake = routeForNodes;
                bakedRoute = EnemyPathMovementBaking.BakeRoute(routeForBake, logicFps);
            }

            if (bakedRoute == null || bakedRoute.legCount == 0)
            {
                visual.sampledPath.Add(spawn);
                PopulateRouteAnnotations(ref visual, routeForBake ?? routeForNodes, logicFps, entryLabel, null);
            }
            else
            {
                FillSampledPathFromBakedRoute(bakedRoute, spawn, visual.sampledPath, maxPathSamples, loopRoute: false);
                FinalizeSpawnPathExit(area, ref visual);
                PopulateRouteAnnotations(ref visual, routeForBake ?? routeForNodes, logicFps, entryLabel, bakedRoute);
            }

            result.Add(visual);
        }

        return result;
    }

    static void PopulateRouteAnnotations(
        ref SpawnPathVisual visual,
        PathRouteMovementData route,
        uint logicFps,
        string routeTitle,
        BakedPathRoute bakedRoute)
    {
        visual.routeHeader = BuildRouteHeader(route, logicFps, routeTitle, bakedRoute);
    }

    static string BuildRouteHeader(
        PathRouteMovementData route,
        uint logicFps,
        string entryLabel,
        BakedPathRoute bakedRoute)
    {
        route?.EnsureSpawnAnchoredFormat();
        if (!PathRouteMovementData.HasAnyPathContent(route))
            return string.IsNullOrEmpty(entryLabel) ? "—" : entryLabel;

        float fps = Mathf.Max(1f, logicFps);
        float totalSec = bakedRoute != null && bakedRoute.durationFrames > 0
            ? bakedRoute.durationFrames / fps
            : 0f;
        if (totalSec > 0.01f)
            return $"{entryLabel} {totalSec:F1}s";
        return entryLabel;
    }

    /// <summary>沿采样路径求首次离开 GO 回收矩形的边界交点（与 <see cref="EnemyMovementSystem"/> 退场一致）。</summary>
    public static bool TryFindLeaveRecycleCrossing(
        in BattleAreaData area,
        IReadOnlyList<Vector2> path,
        out Vector2 crossing)
    {
        crossing = default;
        if (path == null || path.Count < 2)
            return false;

        float bestT = float.MaxValue;
        Vector2 best = default;
        bool found = false;

        for (int i = 1; i < path.Count; i++)
        {
            if (!TryFindLeaveRecycleCrossingOnSegment(area, path[i - 1], path[i], out Vector2 hit, out float t))
                continue;
            if (!found || t < bestT)
            {
                bestT = t;
                best = hit;
                found = true;
            }
        }

        if (!found)
            return false;

        crossing = best;
        return true;
    }

    static void FinalizeSpawnPathExit(in BattleAreaData area, ref SpawnPathVisual visual, bool loopRoute = false)
    {
        visual.hasExit = !loopRoute && TryResolveLeaveRecycleCrossing(area, visual, out visual.exit);
    }

    /// <summary>回收交点：仅当采样/关键点折线真实穿出 GO 回收矩形时返回（不外推末段方向）。</summary>
    static bool TryResolveLeaveRecycleCrossing(
        in BattleAreaData area,
        in SpawnPathVisual visual,
        out Vector2 crossing)
    {
        if (TryFindLeaveRecycleCrossing(area, visual.sampledPath, out crossing))
            return true;

        var keypoints = BuildWorldKeypointPath(visual);
        return keypoints.Count >= 2 && TryFindLeaveRecycleCrossing(area, keypoints, out crossing);
    }

    static List<Vector2> BuildWorldKeypointPath(in SpawnPathVisual visual)
    {
        var pts = new List<Vector2> { visual.spawn };
        if (visual.nodes == null)
            return pts;

        for (int i = 0; i < visual.nodes.Count; i++)
            pts.Add(visual.nodes[i].worldPos);
        return pts;
    }

    static bool TryFindLeaveRecycleCrossingOnSegment(
        in BattleAreaData area,
        Vector2 a,
        Vector2 b,
        out Vector2 hit,
        out float tAlongSegment)
    {
        hit = default;
        tAlongSegment = float.MaxValue;
        bool aIn = area.IsPointInRecycleArea(a.x, a.y);
        bool bIn = area.IsPointInRecycleArea(b.x, b.y);
        if (!aIn || bIn)
            return false;

        Vector2 d = b - a;
        float bestT = float.MaxValue;
        Vector2 best = default;

        ConsiderRecycleVerticalEdge(a, d, area.RecycleLeft, area.RecycleBottom, area.RecycleTop, ref bestT, ref best);
        ConsiderRecycleVerticalEdge(a, d, area.RecycleRight, area.RecycleBottom, area.RecycleTop, ref bestT, ref best);
        ConsiderRecycleHorizontalEdge(a, d, area.RecycleBottom, area.RecycleLeft, area.RecycleRight, ref bestT, ref best);
        ConsiderRecycleHorizontalEdge(a, d, area.RecycleTop, area.RecycleLeft, area.RecycleRight, ref bestT, ref best);

        if (bestT > 1f || bestT <= 0f)
            return false;

        hit = best;
        tAlongSegment = bestT;
        return true;
    }

    static void ConsiderRecycleVerticalEdge(
        Vector2 a, Vector2 d, float xEdge, float minY, float maxY, ref float bestT, ref Vector2 best)
    {
        if (Mathf.Abs(d.x) < 1e-6f)
            return;

        float t = (xEdge - a.x) / d.x;
        if (t <= 0f || t >= bestT)
            return;

        float y = a.y + t * d.y;
        if (y < minY - 1e-4f || y > maxY + 1e-4f)
            return;

        bestT = t;
        best = new Vector2(xEdge, y);
    }

    static void ConsiderRecycleHorizontalEdge(
        Vector2 a, Vector2 d, float yEdge, float minX, float maxX, ref float bestT, ref Vector2 best)
    {
        if (Mathf.Abs(d.y) < 1e-6f)
            return;

        float t = (yEdge - a.y) / d.y;
        if (t <= 0f || t >= bestT)
            return;

        float x = a.x + t * d.x;
        if (x < minX - 1e-4f || x > maxX + 1e-4f)
            return;

        bestT = t;
        best = new Vector2(x, yEdge);
    }

    static List<PathNodeVisual> BuildNodeVisuals(PathRouteMovementData route, Vector2 spawnWorld)
    {
        var nodes = new List<PathNodeVisual>();
        if (route?.nodes == null || route.nodes.Count == 0)
            return nodes;

        route.EnsureSpawnAnchoredFormat();
        int last = route.nodes.Count - 1;
        for (int n = 0; n < route.nodes.Count; n++)
        {
            var cfg = route.nodes[n];
            nodes.Add(new PathNodeVisual
            {
                worldPos = spawnWorld + cfg.positionLocal,
                nodeIndex = n,
                holdSeconds = cfg.holdSeconds,
                isEnd = n == last
            });
        }

        return nodes;
    }

    /// <summary>按烘焙路段（停留/直线/弧/贝塞尔/正弦）密采样，与运行时 <see cref="EnemyPathMotionEvaluator"/> 一致。</summary>
    static void FillSampledPathFromBakedRoute(
        BakedPathRoute baked,
        Vector2 spawn,
        List<Vector2> outPoints,
        int maxPoints,
        bool loopRoute)
    {
        outPoints.Clear();
        if (baked == null || baked.legCount == 0)
        {
            outPoints.Add(spawn);
            return;
        }

        AppendSamplePoint(outPoints, spawn, maxPoints);

        float segStart = 0f;
        for (int li = 0; li < baked.legCount; li++)
        {
            BakedPathLeg leg = baked.legs[li];
            float segEnd = leg.endFrame;
            float segLen = Mathf.Max(1f, segEnd - segStart);
            bool isHold = IsBakedHoldLeg(leg);
            int samples = isHold
                ? Mathf.Clamp(Mathf.RoundToInt(segLen), 2, SamplesPerHoldLeg)
                : SamplesPerTravelLeg;

            for (int s = 1; s <= samples; s++)
            {
                float age = segStart + segLen * (s / (float)samples);
                EnemyPathMotionEvaluator.EvaluateAtAge(
                    baked, age, spawn.x, spawn.y, loopRoute, out float x, out float y);
                if (!AppendSamplePoint(outPoints, new Vector2(x, y), maxPoints))
                    return;
            }

            segStart = segEnd;
        }
    }

    static bool AppendSamplePoint(List<Vector2> points, Vector2 p, int maxPoints)
    {
        if (points.Count > 0 && (points[points.Count - 1] - p).sqrMagnitude < 1e-8f)
            return points.Count < maxPoints;

        points.Add(p);
        return points.Count < maxPoints;
    }

    static bool IsBakedHoldLeg(in BakedPathLeg leg)
    {
        if (leg.kind == BakedPathLeg.KindSineOnChord)
            return false;

        const float eps = 1e-4f;
        return Mathf.Abs(leg.p0x - leg.p3x) < eps
               && Mathf.Abs(leg.p0y - leg.p3y) < eps
               && Mathf.Abs(leg.p1x - leg.p3x) < eps
               && Mathf.Abs(leg.p2x - leg.p3x) < eps;
    }

#if UNITY_EDITOR
    public static void DrawBattleAreaFrames(in BattleAreaData area)
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.35f, 0.85f);
        Gizmos.DrawWireCube(
            new Vector3(area.Center.x, area.Center.y, 0f),
            new Vector3(area.Width, area.Height, 0f));

        Gizmos.color = new Color(1f, 0.35f, 0.3f, 0.75f);
        float rw = area.RecycleRight - area.RecycleLeft;
        float rh = area.RecycleTop - area.RecycleBottom;
        var recycleCenter = new Vector3(
            (area.RecycleLeft + area.RecycleRight) * 0.5f,
            (area.RecycleBottom + area.RecycleTop) * 0.5f,
            0f);
        Gizmos.DrawWireCube(recycleCenter, new Vector3(rw, rh, 0f));
    }

    public static void DrawEditorWavePathPreview(
        EnemyWaveConfig wave,
        in BattleAreaData area,
        int waveIndex,
        uint logicFps,
        int pathEditEntryIndex)
    {
        if (wave == null)
            return;

        int activeEntry = wave.ResolvePathDisplayEntryIndex(pathEditEntryIndex);
        DrawFormationSpawnMarkers(wave, area, waveIndex, activeEntry);

        var visuals = BuildEditorPathVisuals(wave, area, waveIndex, logicFps);
        DrawSpawnPathVisuals(visuals, activeEntry, drawSpawnMarker: false);
    }

    static void DrawFormationSpawnMarkers(
        EnemyWaveConfig wave,
        in BattleAreaData area,
        int waveIndex,
        int emphasizeQueueEntryIndex)
    {
        wave.EnsureSpawnQueueMigrated();
        int entryCount = wave.ResolveSpawnCount();
        if (entryCount <= 0)
            return;

        int active = Mathf.Clamp(emphasizeQueueEntryIndex, 0, entryCount - 1);

        for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            if (!EnemyWaveSpawnMath.TryResolveQueueEntrySpawn(wave, area, waveIndex, 0, entryIndex, out Vector2 spawn))
                continue;

            bool emphasize = entryIndex == active;
            var spawn3 = new Vector3(spawn.x, spawn.y, 0f);
            float handleSize = HandleUtility.GetHandleSize(spawn3) * (emphasize ? 0.14f : 0.1f);

            Handles.color = emphasize
                ? new Color(1f, 0.92f, 0.2f, 0.95f)
                : new Color(1f, 0.92f, 0.2f, 0.45f);
            Handles.SphereHandleCap(0, spawn3, Quaternion.identity, handleSize, EventType.Repaint);

            if (entryCount > 1)
            {
                string label = emphasize ? $"#{entryIndex + 1} ▶" : $"#{entryIndex + 1}";
                Handles.Label(spawn3 + Vector3.up * (handleSize * 0.75f), label);
            }
        }
    }

    public static void DrawSpawnPathVisuals(
        IReadOnlyList<SpawnPathVisual> spawns,
        int emphasizeQueueEntryIndex = 0,
        bool drawSpawnMarker = true)
    {
        if (spawns == null || spawns.Count == 0)
            return;

        for (int i = 0; i < spawns.Count; i++)
        {
            var s = spawns[i];
            bool emphasize = s.queueEntryIndex == emphasizeQueueEntryIndex || spawns.Count == 1;
            Color pathLineColor = ResolveEntryPathColor(s.queueEntryIndex, spawns.Count, emphasize);

            if (drawSpawnMarker)
            {
                Gizmos.color = emphasize
                    ? new Color(1f, 0.92f, 0.2f, 0.95f)
                    : new Color(1f, 0.92f, 0.2f, 0.4f);
                Gizmos.DrawSphere(new Vector3(s.spawn.x, s.spawn.y, 0f), emphasize ? 0.12f : 0.08f);
                DrawSpawnSlotLabel(s, spawns.Count, emphasize);
            }

            DrawPathNodes(s.nodes, emphasize);

            if (s.sampledPath != null && s.sampledPath.Count >= 2)
            {
                Gizmos.color = pathLineColor;
                for (int j = 1; j < s.sampledPath.Count; j++)
                {
                    var a = s.sampledPath[j - 1];
                    var b = s.sampledPath[j];
                    Gizmos.DrawLine(new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f));
                }
            }

            if (emphasize)
                DrawRoutePathLabels(s, pathLineColor);

            if (s.hasExit)
                DrawRecycleExitMarker(s.exit, emphasize, pathLineColor, drawLabel: emphasize);
        }
    }

    static Color ResolveEntryPathColor(int entryIndex, int total, bool emphasize)
    {
        float hue = total <= 1 ? 0.55f : (entryIndex * 0.6180339887f) % 1f;
        Color c = Color.HSVToRGB(hue, 0.55f, emphasize ? 0.95f : 0.7f);
        c.a = emphasize ? 0.92f : 0.38f;
        return c;
    }

    static void DrawSpawnSlotLabel(SpawnPathVisual s, int entryCount, bool emphasize)
    {
        var spawn3 = new Vector3(s.spawn.x, s.spawn.y, 0f);
        float handleSize = HandleUtility.GetHandleSize(spawn3);
        Handles.color = new Color(1f, 0.92f, 0.2f, emphasize ? 0.95f : 0.55f);
        string text = entryCount > 1
            ? (emphasize ? $"#{s.queueEntryIndex + 1} ▶" : $"#{s.queueEntryIndex + 1}")
            : "刷怪点";
        Handles.Label(spawn3 + Vector3.up * (handleSize * 0.16f), text);
    }

    static void DrawRoutePathLabels(SpawnPathVisual visual, Color lineColor)
    {
        if (string.IsNullOrEmpty(visual.routeHeader) || visual.sampledPath == null || visual.sampledPath.Count == 0)
            return;

        Handles.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.9f);
        Vector2 mid = ResolveSampledPathMidpoint(visual.sampledPath);
        var mid3 = new Vector3(mid.x, mid.y, 0f);
        float handleSize = HandleUtility.GetHandleSize(mid3);
        Handles.Label(mid3 + Vector3.up * (handleSize * 0.1f), visual.routeHeader);
    }

    static Vector2 ResolveSampledPathMidpoint(IReadOnlyList<Vector2> path)
    {
        if (path == null || path.Count == 0)
            return default;
        return path[path.Count / 2];
    }

    static void DrawRecycleExitMarker(Vector2 exit, bool emphasize, Color? tint = null, bool drawLabel = true)
    {
        var exit3 = new Vector3(exit.x, exit.y, 0f);
        Color c = tint ?? new Color(1f, 0.35f, 0.95f, emphasize ? 0.95f : 0.5f);
        Gizmos.color = c;
        Gizmos.DrawSphere(exit3, emphasize ? 0.11f : 0.08f);
        if (!drawLabel)
            return;

        Handles.color = c;
        float handleSize = HandleUtility.GetHandleSize(exit3);
        Handles.Label(exit3 + Vector3.up * (handleSize * 0.12f), "退场");
    }

    static void DrawPathNodes(List<PathNodeVisual> nodes, bool emphasize, bool drawLabels = true)
    {
        if (nodes == null)
            return;

        float alpha = emphasize ? 0.95f : 0.42f;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            Color c = n.isEnd
                ? new Color(1f, 0.55f, 0.15f, alpha)
                : new Color(0.25f, 0.95f, 0.55f, alpha);
            Gizmos.color = c;
            float r = emphasize
                ? (n.isEnd ? 0.14f : 0.11f)
                : (n.isEnd ? 0.1f : 0.08f);
            Gizmos.DrawSphere(new Vector3(n.worldPos.x, n.worldPos.y, 0f), r);

            if (!drawLabels || !emphasize)
                continue;

            Handles.color = c;
            string label = n.isEnd ? "终" : $"P{n.nodeIndex + 1}";
            var pos3 = new Vector3(n.worldPos.x, n.worldPos.y, 0f);
            float handleSize = HandleUtility.GetHandleSize(pos3);
            Handles.Label(pos3 + Vector3.up * (handleSize * 0.12f), label);
        }
    }

    public struct MidBossPathVisual
    {
        public int phaseIndex;
        public string label;
        public Color lineColor;
        public SpawnPathVisual path;
    }

    public static List<MidBossPathVisual> BuildMidBossEditorPathVisuals(
        MidBossEncounterConfig encounter,
        in BattleAreaData area,
        uint logicFps,
        int maxPathSamples = DefaultMaxPathSamples)
    {
        var result = new List<MidBossPathVisual>();
        if (encounter == null || !encounter.enabled)
            return result;

        Vector2 spawn = ResolveMidBossSpawn(encounter, area);
        Vector2 phaseOrigin = spawn;

        if (encounter.entryPathRoute != null)
        {
            AddMidBossRoute(result, 0, "入场", new Color(1f, 0.92f, 0.2f, 0.9f), encounter.entryPathRoute, phaseOrigin, area, logicFps, maxPathSamples, false);
            phaseOrigin = EvaluateRouteEndWorld(encounter.entryPathRoute, phaseOrigin, logicFps, false);
        }

        if (encounter.loopPathRoute != null)
            AddMidBossRoute(result, 1, "循环", new Color(0.35f, 0.75f, 1f, 0.9f), encounter.loopPathRoute, phaseOrigin, area, logicFps, maxPathSamples, true);

        if (encounter.exitPathRoute != null)
            AddMidBossRoute(result, 2, "退场", new Color(1f, 0.35f, 0.95f, 0.9f), encounter.exitPathRoute, phaseOrigin, area, logicFps, maxPathSamples, false);

        return result;
    }

    public static Vector2 ResolveMidBossSpawn(MidBossEncounterConfig encounter, in BattleAreaData area) =>
        area.Center + encounter.spawnOffset + new Vector2(0f, area.Height * encounter.yHeightNorm);

    public static Vector2 EvaluateRouteEndWorld(
        PathRouteMovementData route,
        Vector2 origin,
        uint logicFps,
        bool loopSample)
    {
        if (route == null)
            return origin;

        route.BakeMovementTiming(logicFps);
        var baked = EnemyPathMovementBaking.BakeRoute(route, logicFps);
        if (baked == null || baked.legCount == 0)
            return origin;

        int dur = baked.durationFrames > 0 ? baked.durationFrames : 60;
        EnemyPathMotionEvaluator.TryEvaluate(
            baked, 0, origin.x, origin.y, (uint)dur, loopSample, out float x, out float y);
        return new Vector2(x, y);
    }

    static void AddMidBossRoute(
        List<MidBossPathVisual> result,
        int phaseIndex,
        string label,
        Color lineColor,
        PathRouteMovementData route,
        Vector2 origin,
        in BattleAreaData area,
        uint logicFps,
        int maxPathSamples,
        bool loopSample)
    {
        if (route == null)
            return;

        route.BakeMovementTiming(logicFps);
        var baked = EnemyPathMovementBaking.BakeRoute(route, logicFps);
        if (baked == null || baked.legCount == 0)
            return;

        var visual = new SpawnPathVisual
        {
            spawn = origin,
            queueEntryIndex = 0,
            formationSlotIndex = 0,
            nodes = BuildNodeVisuals(route, origin),
            sampledPath = new List<Vector2>(maxPathSamples + 2)
        };

        FillSampledPathFromBakedRoute(baked, origin, visual.sampledPath, maxPathSamples, loopRoute: loopSample);

        FinalizeSpawnPathExit(area, ref visual, loopSample);
        PopulateRouteAnnotations(ref visual, route, logicFps, label, baked);
        result.Add(new MidBossPathVisual { phaseIndex = phaseIndex, label = label, lineColor = lineColor, path = visual });
    }

    public static void DrawMidBossPathVisuals(
        List<MidBossPathVisual> visuals,
        MidBossEncounterConfig encounter,
        in BattleAreaData area,
        int emphasizePhaseIndex = 0)
    {
        if (visuals == null)
            return;

        for (int i = 0; i < visuals.Count; i++)
        {
            var v = visuals[i];
            bool emphasize = emphasizePhaseIndex < 0
                               || v.phaseIndex == emphasizePhaseIndex
                               || visuals.Count == 1;
            Color lineColor = v.lineColor;
            lineColor.a = emphasize ? 0.92f : 0.38f;

            if (v.path.sampledPath != null && v.path.sampledPath.Count >= 2)
            {
                Gizmos.color = lineColor;
                for (int j = 1; j < v.path.sampledPath.Count; j++)
                {
                    var a = v.path.sampledPath[j - 1];
                    var b = v.path.sampledPath[j];
                    Gizmos.DrawLine(new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f));
                }
            }

            DrawPathNodes(v.path.nodes, emphasize);

            if (emphasize)
            {
                Gizmos.color = lineColor;
                DrawRoutePathLabels(v.path, lineColor);
            }

            if (v.path.hasExit)
                DrawRecycleExitMarker(v.path.exit, emphasize, lineColor, drawLabel: emphasize);
        }

        if (encounter != null && encounter.enabled)
        {
            Vector2 spawn = ResolveMidBossSpawn(encounter, area);
            var spawn3 = new Vector3(spawn.x, spawn.y, 0f);
            float handleSize = HandleUtility.GetHandleSize(spawn3);
            Gizmos.color = new Color(1f, 0.92f, 0.2f, 0.95f);
            Gizmos.DrawSphere(spawn3, 0.14f);
            Handles.color = new Color(1f, 0.92f, 0.2f, 0.95f);
            Handles.Label(spawn3 + Vector3.up * (handleSize * 0.18f), "登场点");
        }
    }

    public struct MainBossPathVisual
    {
        public int phaseIndex;
        public string label;
        public Color lineColor;
        public SpawnPathVisual path;
    }

    public static List<MainBossPathVisual> BuildMainBossEditorPathVisuals(
        MainBossEncounterConfig encounter,
        in BattleAreaData area,
        uint logicFps,
        int maxPathSamples = DefaultMaxPathSamples)
    {
        var result = new List<MainBossPathVisual>();
        if (encounter == null || !encounter.enabled)
            return result;

        Vector2 spawn = ResolveMainBossSpawn(encounter, area);
        Vector2 phaseOrigin = spawn;

        if (encounter.entryPathRoute != null)
        {
            AddMainBossRoute(result, 0, "登场", new Color(1f, 0.92f, 0.2f, 0.9f), encounter.entryPathRoute, phaseOrigin, area, logicFps, maxPathSamples, false);
            phaseOrigin = EvaluateRouteEndWorld(encounter.entryPathRoute, phaseOrigin, logicFps, false);
        }

        if (encounter.loopPathRoute != null)
            AddMainBossRoute(result, 1, "场内", new Color(0.35f, 0.75f, 1f, 0.9f), encounter.loopPathRoute, phaseOrigin, area, logicFps, maxPathSamples, true);

        return result;
    }

    public static Vector2 ResolveMainBossSpawn(MainBossEncounterConfig encounter, in BattleAreaData area) =>
        area.Center + encounter.spawnOffset + new Vector2(0f, area.Height * encounter.yHeightNorm);

    static void AddMainBossRoute(
        List<MainBossPathVisual> result,
        int phaseIndex,
        string label,
        Color lineColor,
        PathRouteMovementData route,
        Vector2 origin,
        in BattleAreaData area,
        uint logicFps,
        int maxPathSamples,
        bool loopSample)
    {
        if (route == null)
            return;

        route.BakeMovementTiming(logicFps);
        var baked = EnemyPathMovementBaking.BakeRoute(route, logicFps);
        if (baked == null || baked.legCount == 0)
            return;

        var visual = new SpawnPathVisual
        {
            spawn = origin,
            queueEntryIndex = 0,
            formationSlotIndex = 0,
            nodes = BuildNodeVisuals(route, origin),
            sampledPath = new List<Vector2>(maxPathSamples + 2)
        };

        FillSampledPathFromBakedRoute(baked, origin, visual.sampledPath, maxPathSamples, loopRoute: loopSample);
        FinalizeSpawnPathExit(area, ref visual, loopSample);
        PopulateRouteAnnotations(ref visual, route, logicFps, label, baked);
        result.Add(new MainBossPathVisual { phaseIndex = phaseIndex, label = label, lineColor = lineColor, path = visual });
    }

    public static void DrawMainBossPathVisuals(
        List<MainBossPathVisual> visuals,
        MainBossEncounterConfig encounter,
        in BattleAreaData area,
        int emphasizePhaseIndex = 0)
    {
        if (visuals == null)
            return;

        for (int i = 0; i < visuals.Count; i++)
        {
            var v = visuals[i];
            bool emphasize = emphasizePhaseIndex < 0
                               || v.phaseIndex == emphasizePhaseIndex
                               || visuals.Count == 1;
            Color lineColor = v.lineColor;
            lineColor.a = emphasize ? 0.92f : 0.38f;

            if (v.path.sampledPath != null && v.path.sampledPath.Count >= 2)
            {
                Gizmos.color = lineColor;
                for (int j = 1; j < v.path.sampledPath.Count; j++)
                {
                    var a = v.path.sampledPath[j - 1];
                    var b = v.path.sampledPath[j];
                    Gizmos.DrawLine(new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f));
                }
            }

            DrawPathNodes(v.path.nodes, emphasize);

            if (emphasize)
                DrawRoutePathLabels(v.path, lineColor);

            if (v.path.hasExit)
                DrawRecycleExitMarker(v.path.exit, emphasize, lineColor, drawLabel: emphasize);
        }

        if (encounter != null && encounter.enabled)
        {
            Vector2 spawn = ResolveMainBossSpawn(encounter, area);
            var spawn3 = new Vector3(spawn.x, spawn.y, 0f);
            float handleSize = HandleUtility.GetHandleSize(spawn3);
            Gizmos.color = new Color(1f, 0.92f, 0.2f, 0.95f);
            Gizmos.DrawSphere(spawn3, 0.14f);
            Handles.color = new Color(1f, 0.92f, 0.2f, 0.95f);
            Handles.Label(spawn3 + Vector3.up * (handleSize * 0.18f), "登场点");
        }
    }

#endif
}
