using UnityEngine;

/// <summary>
/// 评估 <see cref="BakedPathRoute"/> 上的位置（确定性、无分配）。
/// </summary>
public static class EnemyPathMotionEvaluator
{
    public static bool TryEvaluate(
        int routeBakeIndex,
        uint spawnFrame,
        float originX,
        float originY,
        uint currentFrame,
        out float x,
        out float y)
    {
        x = originX;
        y = originY;
        if (!EnemyPathBakeCache.TryGet(routeBakeIndex, out var route) || route.legCount == 0)
            return false;

        uint age = currentFrame - spawnFrame;
        if (age == 0)
            return true;

        int legCount = route.legCount;
        var legs = route.legs;
        float t = age;
        if (route.durationFrames >= 0 && age >= route.durationFrames)
            t = route.durationFrames;

        for (int i = 0; i < legCount; i++)
        {
            BakedPathLeg leg = legs[i];
            if (t < leg.endFrame || i == legCount - 1)
            {
                float segLen = Mathf.Max(1f, i == 0 ? leg.endFrame : leg.endFrame - legs[i - 1].endFrame);
                float segStart = i == 0 ? 0f : legs[i - 1].endFrame;
                float u = Mathf.Clamp01((t - segStart) / segLen);
                CubicBezier(
                    u,
                    leg.p0x + originX, leg.p0y + originY,
                    leg.p1x + originX, leg.p1y + originY,
                    leg.p2x + originX, leg.p2y + originY,
                    leg.p3x + originX, leg.p3y + originY,
                    out x, out y);
                return true;
            }
        }

        BakedPathLeg last = legs[legCount - 1];
        x = last.p3x + originX;
        y = last.p3y + originY;
        return true;
    }

    static void CubicBezier(
        float t,
        float p0x, float p0y, float p1x, float p1y, float p2x, float p2y, float p3x, float p3y,
        out float ox, out float oy)
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
