using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 <see cref="PathRouteMovementData"/> 烘焙为 <see cref="BakedPathRoute"/>。
/// </summary>
public static class EnemyPathMovementBaking
{
    public static BakedPathRoute BakeRoute(PathRouteMovementData data, uint logicFps)
    {
        var route = new BakedPathRoute();
        if (data == null || data.nodes == null || data.nodes.Count < 2)
            return route;

        data.BakeMovementTiming(logicFps);
        float fps = Mathf.Max(1f, logicFps);
        int nodeCount = data.nodes.Count;
        int legConfigCount = data.legs != null ? data.legs.Count : 0;
        int cumulative = 0;

        for (int i = 0; i < nodeCount; i++)
        {
            MovementPathNode node = data.nodes[i];
            Vector2 pos = node.positionLocal;

            int holdFrames = node.holdSeconds > 0f
                ? Mathf.Max(1, Mathf.RoundToInt(node.holdSeconds * fps))
                : 0;
            if (holdFrames > 0)
            {
                cumulative += holdFrames;
                route.legs.Add(MakeHoldLeg(pos, cumulative));
            }

            if (i >= nodeCount - 1)
                continue;

            MovementPathLeg legCfg = default;
            bool hasLeg = data.legs != null && i < legConfigCount;
            if (hasLeg)
                legCfg = data.legs[i];
            Vector2 next = data.nodes[i + 1].positionLocal;
            int travelFrames = ResolveTravelFrames(hasLeg, legCfg, data, pos, next, fps);
            cumulative += travelFrames;
            route.legs.Add(MakeTravelLeg(pos, next, hasLeg, legCfg, cumulative));
        }

        route.legCount = (byte)Mathf.Min(route.legs.Count, byte.MaxValue);
        route.durationFrames = cumulative > 0 ? cumulative : data.durationFrames;
        if (data.durationFrames >= 0 && route.durationFrames < 0)
            route.durationFrames = data.durationFrames;
        return route;
    }

    static int ResolveTravelFrames(bool hasLeg, MovementPathLeg legCfg, PathRouteMovementData data, Vector2 a, Vector2 b, float fps)
    {
        if (hasLeg && legCfg.travelSeconds > 0f)
            return Mathf.Max(1, Mathf.RoundToInt(legCfg.travelSeconds * fps));
        if (data.defaultLegDurationSeconds > 0f)
            return Mathf.Max(1, Mathf.RoundToInt(data.defaultLegDurationSeconds * fps));
        float dist = Vector2.Distance(a, b);
        float speed = Mathf.Max(0.01f, data.moveSpeedPerFrame);
        return Mathf.Max(1, Mathf.CeilToInt(dist / speed));
    }

    static BakedPathLeg MakeHoldLeg(Vector2 pos, int endFrame) => new()
    {
        endFrame = endFrame,
        p0x = pos.x, p0y = pos.y,
        p1x = pos.x, p1y = pos.y,
        p2x = pos.x, p2y = pos.y,
        p3x = pos.x, p3y = pos.y
    };

    static BakedPathLeg MakeTravelLeg(Vector2 a, Vector2 b, bool hasLeg, MovementPathLeg cfg, int endFrame)
    {
        E_PathSegmentCurve curve = hasLeg ? cfg.curve : E_PathSegmentCurve.Linear;
        Vector2 p1, p2;
        switch (curve)
        {
            case E_PathSegmentCurve.Bezier:
                p1 = hasLeg ? a + cfg.bezierHandle1Local : a + (b - a) * 0.33f;
                p2 = hasLeg ? b + cfg.bezierHandle2Local : b - (b - a) * 0.33f;
                break;
            case E_PathSegmentCurve.Arc:
                ArcToBezierHandles(a, b, hasLeg ? cfg.arcBulge : 0.5f, out p1, out p2);
                break;
            default:
                p1 = a + (b - a) * (1f / 3f);
                p2 = b - (b - a) * (1f / 3f);
                break;
        }

        return new BakedPathLeg
        {
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
