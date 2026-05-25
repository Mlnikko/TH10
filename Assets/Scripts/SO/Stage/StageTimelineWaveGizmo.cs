using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 关卡时间轴波次：刷怪点、运动路径采样与 GO 回收退场点（编辑器 Scene 可视化）。
/// </summary>
public static class StageTimelineWaveGizmo
{
    public struct EnemyPathPreview
    {
        public Vector2 spawn;
        public Vector2 exit;
        public bool hasExit;
        public List<Vector2> path;
    }

    const int DefaultMaxPathSamples = 96;
    const int MaxSimFramesCap = 7200;

    public static List<EnemyPathPreview> BuildPathPreviews(
        EnemyWaveConfig wave,
        in BattleAreaData area,
        int waveIndex,
        uint logicFps,
        int maxPathSamples = DefaultMaxPathSamples)
    {
        var result = new List<EnemyPathPreview>();
        if (wave == null)
            return result;

        wave.BakeLogicTiming(logicFps);
        wave.movementData?.BakeMovementTiming(logicFps);
        EnemyPathBakeCache.Clear();
        wave.BakePathRouteIfNeeded(logicFps);

        uint spawnFrame = 0;
        var spawns = EnemyWaveSpawnMath.ComputeSpawnPositions(wave, area, waveIndex, spawnFrame);
        int simFrames = ResolveSimFrameCount(wave, area, logicFps);
        int step = Mathf.Max(1, simFrames / Mathf.Max(1, maxPathSamples));

        for (int i = 0; i < spawns.Count; i++)
        {
            var preview = new EnemyPathPreview
            {
                spawn = spawns[i],
                path = new List<Vector2>(maxPathSamples + 2)
            };

            bool usePathRoute = wave.pathRouteBakeIndex >= 0;
            CEnemyMovement motion = default;
            if (!usePathRoute
                && !EnemyMovementBaking.TryBakeFromWave(wave, spawnFrame, preview.spawn.x, preview.spawn.y, i, out motion))
            {
                preview.path.Add(preview.spawn);
                result.Add(preview);
                continue;
            }

            for (uint f = 0; f <= simFrames; f += (uint)step)
            {
                float x, y;
                if (usePathRoute)
                {
                    if (!EnemyPathMotionEvaluator.TryEvaluate(
                            wave.pathRouteBakeIndex, spawnFrame, preview.spawn.x, preview.spawn.y, spawnFrame + f, out x, out y))
                        continue;
                }
                else
                {
                    EnemyMotionEvaluator.Evaluate(in motion, spawnFrame + f, out x, out y);
                }

                preview.path.Add(new Vector2(x, y));
                if (!preview.hasExit && !area.IsPointInRecycleArea(x, y))
                {
                    preview.exit = new Vector2(x, y);
                    preview.hasExit = true;
                }
            }

            if (!preview.hasExit && preview.path.Count > 0)
            {
                var last = preview.path[preview.path.Count - 1];
                preview.exit = last;
                preview.hasExit = true;
            }

            result.Add(preview);
        }

        return result;
    }

    static int ResolveSimFrameCount(EnemyWaveConfig wave, in BattleAreaData area, uint logicFps)
    {
        if (wave.pathRouteBakeIndex >= 0
            && EnemyPathBakeCache.TryGet(wave.pathRouteBakeIndex, out var route)
            && route.durationFrames > 0)
            return Mathf.Min(MaxSimFramesCap, route.durationFrames + (int)logicFps);

        int dur = wave.movementData != null ? wave.movementData.durationFrames : -1;
        if (dur > 0)
            return Mathf.Min(MaxSimFramesCap, dur + (int)logicFps);

        float speed = wave.movementData != null
            ? Mathf.Max(0.01f, wave.movementData.moveSpeedPerFrame)
            : (wave.useDefaultDescentIfNoMovement ? wave.defaultDescentSpeedPerFrame : 0f);

        if (speed <= 0f)
            return (int)Mathf.Min(MaxSimFramesCap, logicFps * 30);

        float travel = area.GO_RecycleMargin.y * 2f + area.Height;
        int est = Mathf.CeilToInt(travel / speed) + (int)logicFps * 2;
        return Mathf.Clamp(est, (int)logicFps, MaxSimFramesCap);
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

    public static void DrawPathPreviews(IReadOnlyList<EnemyPathPreview> paths)
    {
        if (paths == null || paths.Count == 0)
            return;

        for (int i = 0; i < paths.Count; i++)
        {
            var p = paths[i];

            Gizmos.color = new Color(1f, 0.92f, 0.2f, 0.95f);
            Gizmos.DrawSphere(new Vector3(p.spawn.x, p.spawn.y, 0f), 0.12f);

            if (p.path != null && p.path.Count >= 2)
            {
                Gizmos.color = new Color(0.35f, 0.75f, 1f, 0.9f);
                for (int j = 1; j < p.path.Count; j++)
                {
                    var a = p.path[j - 1];
                    var b = p.path[j];
                    Gizmos.DrawLine(new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f));
                }
            }

            if (p.hasExit)
            {
                Gizmos.color = new Color(1f, 0.35f, 0.95f, 0.95f);
                Gizmos.DrawSphere(new Vector3(p.exit.x, p.exit.y, 0f), 0.1f);
                Handles.color = new Color(1f, 0.35f, 0.95f, 0.85f);
                Handles.DrawDottedLine(
                    new Vector3(p.spawn.x, p.spawn.y, 0f),
                    new Vector3(p.exit.x, p.exit.y, 0f),
                    4f);
            }
        }
    }
#endif
}
