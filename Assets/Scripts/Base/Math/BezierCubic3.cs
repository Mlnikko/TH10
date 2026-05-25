/// <summary>三次贝塞尔曲线求值（确定性，无 Unity 依赖）。</summary>
public static class BezierCubic3
{
    public static void Evaluate(
        float t,
        float p0x, float p0y,
        float p1x, float p1y,
        float p2x, float p2y,
        float p3x, float p3y,
        out float x, out float y)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        x = uuu * p0x + 3f * uu * t * p1x + 3f * u * tt * p2x + ttt * p3x;
        y = uuu * p0y + 3f * uu * t * p1y + 3f * u * tt * p2y + ttt * p3y;
    }
}
