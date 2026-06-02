using System.Text;
using UnityEngine;

/// <summary>道中波次编辑器用自动标题格式。</summary>
public static class EnemyWaveTitleFormat
{
    public static string Build(EnemyWaveConfig wave)
    {
        if (wave == null)
            return string.Empty;

        wave.EnsureSpawnQueueMigrated();
        int enemyCount = wave.ResolveSpawnCount();
        string spawnName = FormatSpawnPattern(wave.spawnPattern);

        PathRouteMovementData route = ResolveTitlePathRoute(wave);
        int nodeCount = route?.nodes?.Count ?? 0;
        string pathChain = FormatPathCurveChain(route);
        float durationSeconds = ResolvePathDurationSeconds(route, wave);

        return $"{enemyCount}个敌人 # 生成阵型：{spawnName} # {nodeCount}个路径点 # 路径：{pathChain} # 总时长：{FormatDuration(durationSeconds)} s";
    }

    static PathRouteMovementData ResolveTitlePathRoute(EnemyWaveConfig wave)
    {
        if (wave.pathAssignment == E_WavePathAssignment.PerQueueEntry)
        {
            var entryRoute = wave.ResolvePathForEntry(0);
            if (EnemyWaveConfig.HasUsablePathRoute(entryRoute))
                return entryRoute;
        }

        return EnemyWaveConfig.HasUsablePathRoute(wave.pathRoute) ? wave.pathRoute : null;
    }

    static float ResolvePathDurationSeconds(PathRouteMovementData route, EnemyWaveConfig wave)
    {
        if (route != null)
            return route.ComputeTotalDurationSeconds();

        if (wave.useDefaultDescentIfNoMovement && wave.defaultDescentSpeed > 0f)
        {
            float descent = wave.defaultDescentSpeed;
            return 48f / descent;
        }

        return 0f;
    }

    static string FormatSpawnPattern(SpawnPattern pattern) => pattern switch
    {
        SpawnPattern.Line => "Line",
        SpawnPattern.Grid => "Grid",
        SpawnPattern.Circle => "Circle",
        SpawnPattern.Random => "Random",
        _ => pattern.ToString()
    };

    static string FormatPathCurveChain(PathRouteMovementData route)
    {
        if (route?.nodes == null || route.nodes.Count == 0)
            return "无";

#if UNITY_EDITOR
        route.EnsureLegsMatchNodeCount();
#endif

        int nodeCount = route.nodes.Count;
        int legCount = route.legs?.Count ?? 0;
        var sb = new StringBuilder();
        for (int i = 0; i < nodeCount; i++)
        {
            if (sb.Length > 0)
                sb.Append("->");

            E_PathSegmentCurve curve = i < legCount
                ? route.legs[i].curve
                : E_PathSegmentCurve.Linear;
            sb.Append(FormatCurve(curve));
        }

        return sb.ToString();
    }

    static string FormatCurve(E_PathSegmentCurve curve) => curve switch
    {
        E_PathSegmentCurve.Linear => "直线",
        E_PathSegmentCurve.Arc => "圆弧",
        E_PathSegmentCurve.Bezier => "贝塞尔",
        E_PathSegmentCurve.Sine => "正弦",
        _ => curve.ToString()
    };

    static string FormatDuration(float seconds)
    {
        if (seconds <= 0f)
            return "0";

        float rounded = Mathf.Round(seconds * 100f) / 100f;
        if (Mathf.Approximately(rounded, Mathf.Round(rounded)))
            return Mathf.RoundToInt(rounded).ToString();

        return rounded.ToString("0.##");
    }
}
