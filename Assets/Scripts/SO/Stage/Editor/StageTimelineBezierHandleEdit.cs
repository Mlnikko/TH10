#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Scene 贝塞尔路径段：控制点可视化与拖拽（与 <see cref="EnemyPathMovementBaking"/> 控制点语义一致）。</summary>
static class StageTimelineBezierHandleEdit
{
    const int CurveSamples = 24;

    static readonly Color CurveColor = new(1f, 0.78f, 0.28f, 0.9f);
    static readonly Color TangentColor = new(1f, 1f, 1f, 0.32f);
    static readonly Color Handle1Color = new(1f, 0.42f, 0.82f, 0.96f);
    static readonly Color Handle2Color = new(0.42f, 0.78f, 1f, 0.96f);

    public static void DrawHandles(
        UnityEngine.Object undoTarget,
        Vector2 spawn,
        in BattleAreaData area,
        PathRouteMovementData route,
        bool snap,
        float snapCell,
        ref bool changed)
    {
        if (route?.nodes == null || route.legs == null)
            return;

        route.EnsureLegsMatchNodeCount();
        int count = Mathf.Min(route.nodes.Count, route.legs.Count);
        for (int i = 0; i < count; i++)
        {
            if (route.legs[i].curve != E_PathSegmentCurve.Bezier)
                continue;

            ResolveSegmentLocal(route, i, out Vector2 fromLocal, out Vector2 toLocal);
            ResolveBezierControlPointsLocal(fromLocal, toLocal, route.legs[i], out Vector2 p1Local, out Vector2 p2Local);

            Vector3 p0 = LocalToWorld(spawn, fromLocal);
            Vector3 p1 = LocalToWorld(spawn, p1Local);
            Vector3 p2 = LocalToWorld(spawn, p2Local);
            Vector3 p3 = LocalToWorld(spawn, toLocal);

            DrawCurveAndTangents(p0, p1, p2, p3);
            DrawDraggableHandle(
                undoTarget, spawn, area, route.legs[i], fromLocal, toLocal,
                handleIndex: 1, p1, snap, snapCell, ref changed);
            DrawDraggableHandle(
                undoTarget, spawn, area, route.legs[i], fromLocal, toLocal,
                handleIndex: 2, p2, snap, snapCell, ref changed);
        }
    }

    static void DrawCurveAndTangents(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Handles.color = TangentColor;
        Handles.DrawDottedLine(p0, p1, 4f);
        Handles.DrawDottedLine(p3, p2, 4f);

        Handles.color = CurveColor;
        Vector3 prev = p0;
        for (int s = 1; s <= CurveSamples; s++)
        {
            float t = s / (float)CurveSamples;
            Vector3 pt = EvaluateCubic(p0, p1, p2, p3, t);
            Handles.DrawLine(prev, pt);
            prev = pt;
        }
    }

    static void DrawDraggableHandle(
        UnityEngine.Object undoTarget,
        Vector2 spawn,
        in BattleAreaData area,
        MovementPathLeg leg,
        Vector2 fromLocal,
        Vector2 toLocal,
        int handleIndex,
        Vector3 handleWorld,
        bool snap,
        float snapCell,
        ref bool changed)
    {
        Color color = handleIndex == 1 ? Handle1Color : Handle2Color;
        Handles.color = color;

        float handleSize = HandleUtility.GetHandleSize(handleWorld) * 0.1f;
        EditorGUI.BeginChangeCheck();
        Vector3 newWorld = Handles.FreeMoveHandle(
            handleWorld, handleSize, Vector3.zero, Handles.SphereHandleCap);
        if (!EditorGUI.EndChangeCheck())
        {
            Handles.Label(handleWorld + Vector3.up * (handleSize * 1.1f), handleIndex == 1 ? "H1" : "H2");
            return;
        }

        Undo.RecordObject(undoTarget, "Move Bezier Handle");
        MaterializeFallbackHandlesIfNeeded(leg, fromLocal, toLocal);
        Vector2 handleLocalAbs = new Vector2(newWorld.x - spawn.x, newWorld.y - spawn.y);
        if (snap)
            handleLocalAbs = StageTimelinePathEditSnap.SnapLocal(handleLocalAbs, spawn, area, snapCell);

        if (handleIndex == 1)
        {
            Vector2 next = handleLocalAbs - fromLocal;
            if (leg.bezierHandle1Local != next)
            {
                leg.bezierHandle1Local = next;
                changed = true;
            }
        }
        else
        {
            Vector2 next = handleLocalAbs - toLocal;
            if (leg.bezierHandle2Local != next)
            {
                leg.bezierHandle2Local = next;
                changed = true;
            }
        }
    }

    static void ResolveSegmentLocal(PathRouteMovementData route, int legIndex, out Vector2 fromLocal, out Vector2 toLocal)
    {
        fromLocal = legIndex == 0 ? Vector2.zero : route.nodes[legIndex - 1].positionLocal;
        toLocal = route.nodes[legIndex].positionLocal;
    }

    static void ResolveBezierControlPointsLocal(
        Vector2 fromLocal,
        Vector2 toLocal,
        MovementPathLeg leg,
        out Vector2 p1Local,
        out Vector2 p2Local)
    {
        Vector2 chord = toLocal - fromLocal;
        bool hasHandle1 = leg.bezierHandle1Local.sqrMagnitude > 1e-8f;
        bool hasHandle2 = leg.bezierHandle2Local.sqrMagnitude > 1e-8f;

        if (!hasHandle1 && !hasHandle2)
        {
            p1Local = fromLocal + chord * (1f / 3f);
            p2Local = toLocal - chord * (1f / 3f);
            return;
        }

        p1Local = hasHandle1 ? fromLocal + leg.bezierHandle1Local : fromLocal + chord * (1f / 3f);
        p2Local = hasHandle2 ? toLocal + leg.bezierHandle2Local : toLocal - chord * (1f / 3f);
    }

    static void MaterializeFallbackHandlesIfNeeded(MovementPathLeg leg, Vector2 fromLocal, Vector2 toLocal)
    {
        Vector2 chord = toLocal - fromLocal;
        if (leg.bezierHandle1Local.sqrMagnitude < 1e-8f && leg.bezierHandle2Local.sqrMagnitude < 1e-8f)
        {
            leg.bezierHandle1Local = chord * (1f / 3f);
            leg.bezierHandle2Local = -chord * (1f / 3f);
            return;
        }

        if (leg.bezierHandle1Local.sqrMagnitude < 1e-8f)
            leg.bezierHandle1Local = chord * (1f / 3f);
        if (leg.bezierHandle2Local.sqrMagnitude < 1e-8f)
            leg.bezierHandle2Local = -chord * (1f / 3f);
    }

    static Vector3 LocalToWorld(Vector2 spawn, Vector2 local) =>
        new Vector3(spawn.x + local.x, spawn.y + local.y, 0f);

    static Vector3 EvaluateCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
    }
}
#endif
