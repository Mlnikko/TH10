using UnityEngine;

/// <summary>
/// 纯函数：由逻辑帧与 <see cref="CEnemyMovement"/> 计算世界坐标（确定性、无分配）。
/// </summary>
public static class EnemyMotionEvaluator
{
    public static void Evaluate(in CEnemyMovement m, uint currentFrame, out float x, out float y)
    {
        x = m.originX;
        y = m.originY;

        if (m.kind == E_EnemyMotionKind.None || m.kind == E_EnemyMotionKind.Static)
            return;

        uint age = currentFrame - m.spawnFrame;
        float t = age;

        int dur = m.durationFrames;
        if (dur >= 0 && age >= dur)
            t = dur;

        switch (m.kind)
        {
            case E_EnemyMotionKind.Linear:
            case E_EnemyMotionKind.AimedLinear:
                x = m.originX + m.dX * t;
                y = m.originY + m.dY * t;
                return;

            case E_EnemyMotionKind.Sine:
            {
                float bx = m.originX + m.dX * t;
                float by = m.originY + m.dY * t;
                float s = Mathf.Sin(m.sineOmega * t + m.sinePhase0);
                x = bx + m.perpX * (m.sineAmp * s);
                y = by + m.perpY * (m.sineAmp * s);
                return;
            }

            case E_EnemyMotionKind.Orbit:
            {
                float ang = m.orbitPhase0 + m.orbitOmega * t;
                x = m.orbitCx + Mathf.Cos(ang) * m.orbitR;
                y = m.orbitCy + Mathf.Sin(ang) * m.orbitR;
                return;
            }

            case E_EnemyMotionKind.CubicBezier:
            {
                float denom = dur > 0 ? (float)dur : Mathf.Max(1f, t);
                float u = Mathf.Clamp01(t / denom);
                CubicBezier(u, 0f, 0f, m.b1x, m.b1y, m.b2x, m.b2y, m.b3x, m.b3y, out float lx, out float ly);
                x = m.originX + lx;
                y = m.originY + ly;
                return;
            }

            case E_EnemyMotionKind.WaypointPolyline:
                EvaluateWaypoint(in m, t, out x, out y);
                return;
        }
    }

    static void EvaluateWaypoint(in CEnemyMovement m, float age, out float x, out float y)
    {
        Vector2 o = new(m.originX, m.originY);
        if (m.wayCount == 0)
        {
            x = o.x;
            y = o.y;
            return;
        }

        Vector2 p1 = new(m.originX + m.wp1x, m.originY + m.wp1y);
        Vector2 p2 = new(m.originX + m.wp2x, m.originY + m.wp2y);
        Vector2 p3 = new(m.originX + m.wp3x, m.originY + m.wp3y);
        Vector2 p4 = new(m.originX + m.wp4x, m.originY + m.wp4y);

        Vector2 Pt(int idx)
        {
            switch (idx)
            {
                case 0: return o;
                case 1: return p1;
                case 2: return p2;
                case 3: return p3;
                default: return p4;
            }
        }

        int[] ends = { m.segEnd0, m.segEnd1, m.segEnd2, m.segEnd3 };
        float startT = 0f;
        int segCount = Mathf.Min(m.wayCount, 4);
        for (int s = 0; s < segCount; s++)
        {
            float endT = ends[s];
            float segLen = Mathf.Max(1f, endT - startT);
            if (age < endT || s == segCount - 1)
            {
                float u = Mathf.Clamp01((age - startT) / segLen);
                Vector2 a = Pt(s);
                Vector2 b = Pt(s + 1);
                x = Mathf.Lerp(a.x, b.x, u);
                y = Mathf.Lerp(a.y, b.y, u);
                return;
            }
            startT = endT;
        }

        Vector2 lastPt = Pt(segCount);
        x = lastPt.x;
        y = lastPt.y;
    }

    static void CubicBezier(float t, float p0x, float p0y, float p1x, float p1y, float p2x, float p2y, float p3x, float p3y, out float ox, out float oy)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        ox = uuu * p0x + 3f * uu * t * p1x + 3f * u * tt * p2x + ttt * p3x;
        oy = uuu * p0y + 3f * uu * t * p1y + 3f * u * tt * p2y + ttt * p3y;
    }
}
