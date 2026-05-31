using System;
using UnityEngine;

/// <summary>
/// 追踪弹幕：恒定速度 + 限角速度转向；按生成时确定的弯曲侧走外弧（长路径）再逼近目标。
/// </summary>
public static class DanmakuHomingLogic
{
    const float MinSpeed = 1e-8f;
    /// <summary>角差小于此值时结束外弧，改用最短路径以确保命中。</summary>
    const float DirectHomingEnterAngleRad = MathF.PI / 4f;

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
    /// 按发射点朝向与目标相对位置确定外弧弯曲侧：目标在朝向左侧为 +1，右侧为 -1。
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

    public static void AdvanceHoming(
        EntityManager em,
        ref CPosition position,
        ref CVelocity velocity,
        ref CRotation rotation,
        ref CDanmakuHoming homing)
    {
        if (!TryResolveTargetIndex(
                em, homing.targetEnemyIndex, position.x, position.y, homing.homingTargetLayerMask, out int targetIdx))
        {
            homing.targetEnemyIndex = -1;
            homing.outerArcActive = 0;
            position.x += velocity.vx;
            position.y += velocity.vy;
            rotation.angleRad = MathF.Atan2(velocity.vy, velocity.vx);
            return;
        }

        if (homing.targetEnemyIndex >= 0 && targetIdx != homing.targetEnemyIndex)
            homing.outerArcActive = 0;

        homing.targetEnemyIndex = targetIdx;

        ref readonly var targetPos = ref em.GetComponentSpan<CPosition>()[targetIdx];
        float toX = targetPos.x - position.x;
        float toY = targetPos.y - position.y;

        float speed = homing.speedPerFrame;
        if (speed < MinSpeed)
        {
            speed = MathF.Sqrt(velocity.vx * velocity.vx + velocity.vy * velocity.vy);
            if (speed < MinSpeed)
                return;
            homing.speedPerFrame = speed;
        }

        float heading = MathF.Atan2(velocity.vy, velocity.vx);
        float targetHeading = MathF.Atan2(toY, toX);
        float shortDelta = NormalizeAngleRad(targetHeading - heading);

        if (homing.outerArcActive != 0)
        {
            float bend = homing.curveBendSign >= 0 ? 1f : -1f;
            if (shortDelta * bend >= 0f || MathF.Abs(shortDelta) <= DirectHomingEnterAngleRad)
                homing.outerArcActive = 0;
        }

        float turn = homing.outerArcActive != 0
            ? ResolveOuterArcTurn(shortDelta, homing.curveBendSign, homing.turnSpeedRadPerFrame)
            : ResolveDirectTurn(shortDelta, homing.turnSpeedRadPerFrame);
        heading += turn;

        velocity.vx = MathF.Cos(heading) * speed;
        velocity.vy = MathF.Sin(heading) * speed;
        position.x += velocity.vx;
        position.y += velocity.vy;
        rotation.angleRad = heading;
    }

    static float NormalizeAngleRad(float angleRad)
    {
        const float twoPi = MathF.PI * 2f;
        while (angleRad > MathF.PI)
            angleRad -= twoPi;
        while (angleRad < -MathF.PI)
            angleRad += twoPi;
        return angleRad;
    }

    static float ResolveDirectTurn(float shortDelta, float maxTurnRad)
    {
        if (MathF.Abs(shortDelta) <= maxTurnRad)
            return shortDelta;
        return MathF.Sign(shortDelta) * maxTurnRad;
    }

    /// <summary>
    /// 外弧阶段：在 bendSign 侧走长弧；与最短转角同号时仍用短差，否则绕外圈。
    /// </summary>
    static float ResolveOuterArcTurn(float shortDelta, sbyte bendSign, float maxTurnRad)
    {
        float bend = bendSign >= 0 ? 1f : -1f;
        float longDelta = shortDelta > 0f
            ? shortDelta - (2f * MathF.PI)
            : shortDelta + (2f * MathF.PI);
        float delta = bend > 0f
            ? (shortDelta >= 0f ? shortDelta : longDelta)
            : (shortDelta <= 0f ? shortDelta : longDelta);

        if (MathF.Abs(delta) <= maxTurnRad)
            return delta;
        return MathF.Sign(delta) * maxTurnRad;
    }
}
