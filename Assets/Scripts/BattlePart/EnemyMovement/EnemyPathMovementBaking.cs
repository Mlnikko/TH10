using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 <see cref="PathRouteMovementData"/> 烘焙为 <see cref="BakedPathRoute"/>。
/// 路径起点为生成点（局部原点），<see cref="PathRouteMovementData.nodes"/> 仅含目标路径点。
/// </summary>
public static class EnemyPathMovementBaking
{
    public static BakedPathRoute BakeRoute(PathRouteMovementData data, uint logicFps)
    {
        var route = new BakedPathRoute();
        if (data == null)
            return route;

        data.EnsureSpawnAnchoredFormat();
        if (data.nodes == null || data.nodes.Count < 1)
        {
            if (data.spawnHoldSeconds <= 0f)
                return route;
        }

        data.BakeMovementTiming(logicFps);
        float fps = Mathf.Max(1f, logicFps);
        int nodeCount = data.nodes != null ? data.nodes.Count : 0;
        int legConfigCount = data.legs != null ? data.legs.Count : 0;
        int cumulative = 0;

        if (data.spawnHoldSeconds > 0f)
        {
            int spawnHoldFrames = Mathf.Max(1, Mathf.RoundToInt(data.spawnHoldSeconds * fps));
            cumulative += spawnHoldFrames;
            route.legs.Add(MakeHoldLeg(Vector2.zero, cumulative));
        }

        for (int i = 0; i < nodeCount; i++)
        {
            MovementPathNode node = data.nodes[i];
            Vector2 pos = node.positionLocal;
            Vector2 from = i == 0 ? Vector2.zero : data.nodes[i - 1].positionLocal;

            if (Vector2.Distance(from, pos) > 1e-6f)
            {
                bool hasLeg = data.legs != null && i < legConfigCount;
                MovementPathLeg legCfg = hasLeg ? data.legs[i] : default;
                int travelFrames = data.ResolveTravelFrames(hasLeg, legCfg, from, pos, fps);
                cumulative += travelFrames;
                route.legs.Add(MakeTravelLeg(from, pos, hasLeg, legCfg, cumulative, fps));
            }

            int holdFrames = node.holdSeconds > 0f
                ? Mathf.Max(1, Mathf.RoundToInt(node.holdSeconds * fps))
                : 0;
            if (holdFrames > 0)
            {
                cumulative += holdFrames;
                route.legs.Add(MakeHoldLeg(pos, cumulative));
            }
        }

        route.legCount = (byte)Mathf.Min(route.legs.Count, byte.MaxValue);
        route.durationFrames = cumulative > 0 ? cumulative : data.durationFrames;
        return route;
    }

    static BakedPathLeg MakeHoldLeg(Vector2 pos, int endFrame) => new()
    {
        kind = BakedPathLeg.KindBezier,
        endFrame = endFrame,
        p0x = pos.x, p0y = pos.y,
        p1x = pos.x, p1y = pos.y,
        p2x = pos.x, p2y = pos.y,
        p3x = pos.x, p3y = pos.y
    };

    static BakedPathLeg MakeTravelLeg(
        Vector2 a, Vector2 b, bool hasLeg, MovementPathLeg cfg, int endFrame, float logicFps)
    {
        E_PathSegmentCurve curve = hasLeg ? cfg.curve : E_PathSegmentCurve.Linear;
        if (curve == E_PathSegmentCurve.Sine)
        {
            float amp = hasLeg ? Mathf.Max(0f, cfg.sineAmplitude) : PathMovementLegDefaults.SineAmplitudeFallback;
            float cyclesAlongChord = hasLeg ? Mathf.Max(0f, cfg.sineHz) : PathMovementLegDefaults.SineHzFallback;
            float phase = hasLeg ? cfg.sinePhaseRad : 0f;
            return new BakedPathLeg
            {
                kind = BakedPathLeg.KindSineOnChord,
                endFrame = endFrame,
                p0x = a.x, p0y = a.y,
                p3x = b.x, p3y = b.y,
                sineAmp = amp,
                sineRadiansPerU = Mathf.PI * 2f * cyclesAlongChord,
                sinePhase0 = phase
            };
        }

        Vector2 p1, p2;
        switch (curve)
        {
            case E_PathSegmentCurve.Bezier:
                p1 = hasLeg ? a + cfg.bezierHandle1Local : a + (b - a) * 0.33f;
                p2 = hasLeg ? b + cfg.bezierHandle2Local : b - (b - a) * 0.33f;
                break;
            case E_PathSegmentCurve.Arc:
                ArcToBezierHandles(a, b, hasLeg ? cfg.arcBulge : PathMovementLegDefaults.ArcBulgeFallback, out p1, out p2);
                break;
            default:
                p1 = a + (b - a) * (1f / 3f);
                p2 = b - (b - a) * (1f / 3f);
                break;
        }

        return new BakedPathLeg
        {
            kind = BakedPathLeg.KindBezier,
            endFrame = endFrame,
            p0x = a.x, p0y = a.y,
            p1x = p1.x, p1y = p1.y,
            p2x = p2.x, p2y = p2.y,
            p3x = b.x, p3y = b.y
        };
    }

    static void ArcToBezierHandles(Vector2 a, Vector2 b, float bulge, out Vector2 p1, out Vector2 p2)
    {
        Vector2 mid = (a + b) * 0.5f;
        Vector2 d = b - a;
        if (d.sqrMagnitude < 1e-8f)
        {
            p1 = a;
            p2 = b;
            return;
        }
        Vector2 perp = new Vector2(-d.y, d.x).normalized * bulge;
        p1 = a + d * (1f / 3f) + perp;
        p2 = b - d * (1f / 3f) + perp;
    }
}
