using UnityEngine;
using System;
/// <summary>
/// 将 <see cref="MovementPatternData"/> / 波次默认值烘焙为运行时 <see cref="CEnemyMovement"/>。
/// </summary>
public static class EnemyMovementBaking
{
    const int DefaultBezierDurationFrames = 360;

    /// <summary>测试或脚本生成敌人时的简单竖直下落。</summary>
    public static CEnemyMovement CreateSimpleDescent(uint spawnFrame, float originX, float originY, float descentPerFrame = 0.06f)
    {
        float d = -Mathf.Abs(descentPerFrame);
        return BakeLinear(spawnFrame, originX, originY, 0f, d, -1);
    }

    public static bool TryBakeFromWave(
        EnemyWaveConfig wave,
        uint spawnFrame,
        float originX,
        float originY,
        int spawnIndexInWave,
        EntityManager em,
        out CEnemyMovement motion)
    {
        if (wave.movementData != null)
            return TryBakeFromProfile(wave.movementData, spawnFrame, originX, originY, spawnIndexInWave, em, out motion);

        if (wave.useDefaultDescentIfNoMovement && wave.defaultDescentSpeedPerFrame > 0f)
        {
            motion = BakeLinear(spawnFrame, originX, originY, 0f, -wave.defaultDescentSpeedPerFrame, -1);
            return true;
        }

        motion = new CEnemyMovement
        {
            kind = E_EnemyMotionKind.Static,
            spawnFrame = spawnFrame,
            originX = originX,
            originY = originY,
            durationFrames = -1
        };
        return true;
    }

    public static bool TryBakeFromProfile(
        MovementPatternData data,
        uint spawnFrame,
        float originX,
        float originY,
        int spawnIndexInWave,
        EntityManager em,
        out CEnemyMovement motion)
    {
        motion = default;
        if (data == null)
            return false;

        switch (data)
        {
            case StaticMovementData:
                motion = new CEnemyMovement
                {
                    kind = E_EnemyMotionKind.Static,
                    spawnFrame = spawnFrame,
                    originX = originX,
                    originY = originY,
                    durationFrames = data.durationFrames
                };
                return true;

            case LinearMovementData:
                BakeDirectionalLinear(data, spawnFrame, originX, originY, out motion);
                return true;

            case AimedLinearMovementData:
                BakeAimed(data, spawnFrame, originX, originY, em, out motion);
                return true;

            case SineMovementData sine:
                BakeSine(sine, spawnFrame, originX, originY, spawnIndexInWave, out motion);
                return true;

            case CircularMovementData circle:
                BakeCircle(circle, spawnFrame, originX, originY, out motion);
                return true;

            case BezierCubicMovementData bez:
                BakeBezier(bez, spawnFrame, originX, originY, out motion);
                return true;

            case WaypointPathMovementData path:
                BakeWaypoint(path, spawnFrame, originX, originY, out motion);
                return true;

            default:
                BakeDirectionalLinear(data, spawnFrame, originX, originY, out motion);
                return true;
        }
    }

    static void BakeDirectionalLinear(MovementPatternData data, uint spawnFrame, float ox, float oy, out CEnemyMovement motion)
    {
        Vector2 dir = data.direction.sqrMagnitude > 1e-8f ? data.direction.normalized : Vector2.down;
        motion = BakeLinear(spawnFrame, ox, oy, dir.x * data.speed, dir.y * data.speed, data.durationFrames);
    }

    static CEnemyMovement BakeLinear(uint spawnFrame, float ox, float oy, float dx, float dy, int dur)
    {
        return new CEnemyMovement
        {
            kind = E_EnemyMotionKind.Linear,
            spawnFrame = spawnFrame,
            originX = ox,
            originY = oy,
            durationFrames = dur,
            dX = dx,
            dY = dy
        };
    }

    static void BakeAimed(MovementPatternData data, uint spawnFrame, float ox, float oy, EntityManager em, out CEnemyMovement motion)
    {
        Vector2 dir = Vector2.down;
        if (TryGetFirstPlayerPosition(em, out float px, out float py))
        {
            Vector2 d = new Vector2(px - ox, py - oy);
            if (d.sqrMagnitude > 1e-8f)
                dir = d.normalized;
        }
        motion = BakeLinear(spawnFrame, ox, oy, dir.x * data.speed, dir.y * data.speed, data.durationFrames);
        motion.kind = E_EnemyMotionKind.AimedLinear;
    }

    static bool TryGetFirstPlayerPosition(EntityManager em, out float px, out float py)
    {
        px = py = 0f;
        Span<int> pl = em.GetActiveIndices<CPlayer>();
        if (pl.Length == 0) return false;
        int i = pl[0];
        var posSpan = em.GetComponentSpan<CPosition>();
        if (i < 0 || i >= posSpan.Length) return false;
        var p = posSpan[i];
        px = p.x;
        py = p.y;
        return true;
    }

    static void BakeSine(SineMovementData data, uint spawnFrame, float ox, float oy, int spawnIndexInWave, out CEnemyMovement motion)
    {
        Vector2 dir = data.direction.sqrMagnitude > 1e-8f ? data.direction.normalized : Vector2.down;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float phase = data.phase0Rad + spawnIndexInWave * 0.17f;
        motion = new CEnemyMovement
        {
            kind = E_EnemyMotionKind.Sine,
            spawnFrame = spawnFrame,
            originX = ox,
            originY = oy,
            durationFrames = data.durationFrames,
            dX = dir.x * data.speed,
            dY = dir.y * data.speed,
            perpX = perp.x,
            perpY = perp.y,
            sineAmp = Mathf.Max(0f, data.amplitude),
            sineOmega = Mathf.Max(0f, data.frequency),
            sinePhase0 = phase
        };
    }

    static void BakeCircle(CircularMovementData data, uint spawnFrame, float ox, float oy, out CEnemyMovement motion)
    {
        float cx = ox + data.centerOffset.x;
        float cy = oy + data.centerOffset.y;
        float dx = ox - cx;
        float dy = oy - cy;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        float r = dist > 1e-4f ? dist : Mathf.Max(0.01f, data.radius);
        float phase0 = dist > 1e-4f
            ? Mathf.Atan2(dy, dx)
            : data.startAngleDeg * Mathf.Deg2Rad;

        motion = new CEnemyMovement
        {
            kind = E_EnemyMotionKind.Orbit,
            spawnFrame = spawnFrame,
            originX = ox,
            originY = oy,
            durationFrames = data.durationFrames,
            orbitCx = cx,
            orbitCy = cy,
            orbitR = r,
            orbitOmega = data.angularVelocityRadPerFrame,
            orbitPhase0 = phase0
        };
    }

    static void BakeBezier(BezierCubicMovementData data, uint spawnFrame, float ox, float oy, out CEnemyMovement motion)
    {
        var pts = data.controlPointsLocal != null && data.controlPointsLocal.Count >= 4
            ? data.controlPointsLocal
            : data.bezierPoints;
        Vector2 p0 = Vector2.zero, p1, p2, p3;
        if (pts != null && pts.Count >= 4)
        {
            p0 = pts[0];
            p1 = pts[1];
            p2 = pts[2];
            p3 = pts[3];
        }
        else if (pts != null && pts.Count == 3)
        {
            p0 = Vector2.zero;
            p1 = pts[0];
            p2 = pts[1];
            p3 = pts[2];
        }
        else
        {
            p0 = Vector2.zero;
            p1 = new Vector2(0.2f, -0.5f);
            p2 = new Vector2(0.6f, -1f);
            p3 = new Vector2(1f, -1.5f);
        }

        int dur = data.durationFrames >= 0 ? data.durationFrames : DefaultBezierDurationFrames;
        motion = new CEnemyMovement
        {
            kind = E_EnemyMotionKind.CubicBezier,
            spawnFrame = spawnFrame,
            originX = ox,
            originY = oy,
            durationFrames = dur,
            b1x = p1.x - p0.x,
            b1y = p1.y - p0.y,
            b2x = p2.x - p0.x,
            b2y = p2.y - p0.y,
            b3x = p3.x - p0.x,
            b3y = p3.y - p0.y
        };
    }

    static void BakeWaypoint(WaypointPathMovementData data, uint spawnFrame, float ox, float oy, out CEnemyMovement motion)
    {
        motion = new CEnemyMovement
        {
            kind = E_EnemyMotionKind.WaypointPolyline,
            spawnFrame = spawnFrame,
            originX = ox,
            originY = oy,
            durationFrames = data.durationFrames
        };

        var wps = data.waypointsLocal;
        if (wps == null || wps.Count == 0)
        {
            motion.wayCount = 0;
            return;
        }

        int n = Mathf.Min(wps.Count, 4);
        motion.wayCount = (byte)n;
        if (n >= 1) { motion.wp1x = wps[0].x; motion.wp1y = wps[0].y; }
        if (n >= 2) { motion.wp2x = wps[1].x; motion.wp2y = wps[1].y; }
        if (n >= 3) { motion.wp3x = wps[2].x; motion.wp3y = wps[2].y; }
        if (n >= 4) { motion.wp4x = wps[3].x; motion.wp4y = wps[3].y; }

        var seg = data.segmentFrames;
        int totalDur = 0;
        for (int i = 0; i < n; i++)
        {
            int f = 60;
            if (seg != null && seg.Count > 0)
            {
                if (i < seg.Count)
                    f = Mathf.Max(1, seg[i]);
                else
                    f = Mathf.Max(1, seg[seg.Count - 1]);
            }
            else if (data.durationFrames > 0)
                f = Mathf.Max(1, data.durationFrames / Mathf.Max(1, n));
            totalDur += f;
            switch (i)
            {
                case 0: motion.segEnd0 = totalDur; break;
                case 1: motion.segEnd1 = totalDur; break;
                case 2: motion.segEnd2 = totalDur; break;
                case 3: motion.segEnd3 = totalDur; break;
            }
        }
        if (motion.durationFrames < 0 && totalDur > 0)
            motion.durationFrames = totalDur;
    }

}
