using System.Collections.Generic;

/// <summary>
/// 波次 <see cref="PathRouteMovementData"/> 烘焙结果缓存（按时间轴 Begin 重建，敌人共享只读路线）。
/// </summary>
public static class EnemyPathBakeCache
{
    static readonly List<BakedPathRoute> s_routes = new();

    public static void Clear() => s_routes.Clear();

    public static int Register(BakedPathRoute route)
    {
        if (route == null || route.legCount == 0)
            return -1;
        s_routes.Add(route);
        return s_routes.Count - 1;
    }

    public static bool TryGet(int index, out BakedPathRoute route)
    {
        route = null;
        if (index < 0 || index >= s_routes.Count)
            return false;
        route = s_routes[index];
        return route != null;
    }
}

/// <summary>一条路径由若干「三次贝塞尔段」组成；直线/圆弧在烘焙时已转为等效贝塞尔。</summary>
public sealed class BakedPathRoute
{
    public byte legCount;
    public int durationFrames = -1;
    public readonly List<BakedPathLeg> legs = new();
}

public struct BakedPathLeg
{
    /// <summary>相对 spawnFrame 的累计结束帧（不含）。</summary>
    public int endFrame;
    public float p0x, p0y, p1x, p1y, p2x, p2y, p3x, p3y;
}
