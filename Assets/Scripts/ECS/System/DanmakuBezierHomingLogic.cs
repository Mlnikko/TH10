using System;
using UnityEngine;

/// <summary>
/// 追踪弹幕：每逻辑帧沿当前位置→目标的 cubic Bezier 推进，目标为掩码内最近实体（同距取索引较小）。
/// 控制点同侧偏移，形成相对弦线的外弧（而非 S 形内弯）。
/// </summary>
public static class DanmakuBezierHomingLogic
{
    const float MinChordLength = 0.0001f;
    const float ArrivalThreshold = 0.05f;

    public static int FindNearestTargetIndex(
        EntityManager em,
        float fromX,
        float fromY,
        ushort targetLayerMask)
    {
        if (targetLayerMask == 0)
            return -1;

        int bestIdx = -1;
        float bestDistSq = float.MaxValue;

        if ((targetLayerMask & (ushort)E_ColliderLayer.Enemy) != 0)
            TryPickNearest(em, em.GetActiveIndices<CEnemy>(), fromX, fromY, targetLayerMask, ref bestIdx, ref bestDistSq);

        if ((targetLayerMask & (ushort)E_ColliderLayer.Player) != 0)
            TryPickNearest(em, em.GetActiveIndices<CPlayer>(), fromX, fromY, targetLayerMask, ref bestIdx, ref bestDistSq);

        return bestIdx;
    }

    static void TryPickNearest(
        EntityManager em,
        Span<int> candidates,
        float fromX,
        float fromY,
        ushort targetLayerMask,
        ref int bestIdx,
        ref float bestDistSq)
    {
        var positions = em.GetComponentSpan<CPosition>();
        var colliders = em.GetComponentSpan<CCollider>();

        for (int i = 0; i < candidates.Length; i++)
        {
            int idx = candidates[i];
            if ((uint)idx >= (uint)positions.Length)
                continue;

            ref readonly var col = ref colliders[idx];
            if (!col.isActive)
                continue;
            if (((ushort)col.layer & targetLayerMask) == 0)
                continue;

            ref readonly var pos = ref positions[idx];
            float dx = pos.x - fromX;
            float dy = pos.y - fromY;
            float distSq = dx * dx + dy * dy;

            if (distSq < bestDistSq || (distSq == bestDistSq && idx < bestIdx))
            {
                bestDistSq = distSq;
                bestIdx = idx;
            }
        }
    }

    static bool IsTargetStillValid(
        EntityManager em,
        int targetIndex,
        ushort targetLayerMask)
    {
        if (targetIndex < 0 || !em.IsIndexActive(targetIndex))
            return false;

        ref readonly var col = ref em.GetComponentSpan<CCollider>()[targetIndex];
        return col.isActive && ((ushort)col.layer & targetLayerMask) != 0;
    }

    public static bool TryResolveTargetIndex(
        EntityManager em,
        int currentTargetIndex,
        float fromX,
        float fromY,
        ushort targetLayerMask,
        out int resolvedIndex)
    {
        if (IsTargetStillValid(em, currentTargetIndex, targetLayerMask))
        {
            resolvedIndex = currentTargetIndex;
            return true;
        }

        resolvedIndex = FindNearestTargetIndex(em, fromX, fromY, targetLayerMask);
        return resolvedIndex >= 0;
    }

    /// <summary>
    /// 按发射点朝向与目标相对位置确定 Bezier 弯曲侧：目标在朝向左侧为 +1，右侧为 -1。
    /// </summary>
    public static sbyte ResolveCurveBendSign(
        EntityManager em,
        float emitX,
        float emitY,
        float forwardX,
        float forwardY,
        int targetIndex,
        ushort targetLayerMask)
    {
        if (targetIndex < 0)
            targetIndex = FindNearestTargetIndex(em, emitX, emitY, targetLayerMask);

        if (targetIndex < 0)
            return 1;

        ref readonly var targetPos = ref em.GetComponentSpan<CPosition>()[targetIndex];
        float toX = targetPos.x - emitX;
        float toY = targetPos.y - emitY;

        float forwardLenSq = forwardX * forwardX + forwardY * forwardY;
        if (forwardLenSq < 1e-8f)
            return toX >= 0f ? (sbyte)-1 : (sbyte)1;

        float cross = forwardX * toY - forwardY * toX;
        if (MathF.Abs(cross) < 1e-6f)
            return 1;

        return cross > 0f ? (sbyte)1 : (sbyte)-1;
    }

    public static void AdvanceAlongBezier(
        EntityManager em,
        int entityIndex,
        ref CPosition position,
        ref CVelocity velocity,
        ref CRotation rotation,
        ref CDanmakuBezierHoming homing)
    {
        if (!TryResolveTargetIndex(
                em, homing.targetEnemyIndex, position.x, position.y, homing.homingTargetLayerMask, out int targetIdx))
        {
            homing.targetEnemyIndex = -1;
            homing.segmentT = 0f;
            position.x += velocity.vx;
            position.y += velocity.vy;
            rotation.angleRad = MathF.Atan2(velocity.vy, velocity.vx);
            return;
        }

        homing.targetEnemyIndex = targetIdx;

        ref readonly var targetPos = ref em.GetComponentSpan<CPosition>()[targetIdx];
        float p0x = position.x;
        float p0y = position.y;
        float p3x = targetPos.x;
        float p3y = targetPos.y;

        float dx = p3x - p0x;
        float dy = p3y - p0y;
        float distSq = dx * dx + dy * dy;

        if (distSq < ArrivalThreshold * ArrivalThreshold)
        {
            velocity.vx = dx;
            velocity.vy = dy;
            homing.segmentT = 0f;
            rotation.angleRad = distSq > MinChordLength * MinChordLength
                ? MathF.Atan2(dy, dx)
                : rotation.angleRad;
            return;
        }

        float dist = MathF.Sqrt(distSq);
        float invDist = 1f / dist;
        float nx = dx * invDist;
        float ny = dy * invDist;
        float perpX = -ny;
        float perpY = nx;
        float bendSign = homing.curveBendSign == 0 ? 1f : homing.curveBendSign;
        float bend = homing.curveStrength * dist * 0.25f * bendSign;

        float p1x = p0x + dx * 0.33f + perpX * bend;
        float p1y = p0y + dy * 0.33f + perpY * bend;
        float p2x = p0x + dx * 0.66f + perpX * bend;
        float p2y = p0y + dy * 0.66f + perpY * bend;

        float nextT = homing.segmentT + homing.progressPerFrame;
        if (nextT > 1f)
            nextT = 1f;

        BezierCubic3.Evaluate(nextT, p0x, p0y, p1x, p1y, p2x, p2y, p3x, p3y, out float newX, out float newY);

        velocity.vx = newX - p0x;
        velocity.vy = newY - p0y;
        position.x = newX;
        position.y = newY;
        rotation.angleRad = MathF.Atan2(velocity.vy, velocity.vx);

        homing.segmentT = nextT >= 1f ? 0f : nextT;
    }
}
