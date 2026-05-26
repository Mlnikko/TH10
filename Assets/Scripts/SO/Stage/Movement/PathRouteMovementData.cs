using System;

using System.Collections.Generic;

using UnityEngine;



/// <summary>路径段曲线类型（相邻节点之间的运动方式）。</summary>

public enum E_PathSegmentCurve : byte

{

    Linear = 0,

    Arc = 1,

    Bezier = 2,

    /// <summary>沿弦线方向匀速前进，同时垂直于弦做正弦摆动。</summary>

    Sine = 3

}



[Serializable]

public class MovementPathNode

{

    [Tooltip("相对生成点的局部坐标（生成点本身不在此列表中）")]

    public Vector2 positionLocal;



    [Min(0f)]

    [Tooltip("到达该路径点后的停留时间（秒）")]

    public float holdSeconds;

}



[Serializable]

public class MovementPathLeg

{

    public E_PathSegmentCurve curve = E_PathSegmentCurve.Linear;



    [Min(0f)]

    [Tooltip("本段行驶时间（秒）；≤0 时用默认段时长或按 moveSpeed 推算")]

    public float travelSeconds;



    [Tooltip("贝塞尔：相对段起点的控制点 1")]

    public Vector2 bezierHandle1Local;



    [Tooltip("贝塞尔：相对段终点的控制点 2")]

    public Vector2 bezierHandle2Local;



    [Tooltip("圆弧：中段垂直于弦的拱高（世界单位，可正可负）")]

    public float arcBulge = PathMovementLegDefaults.ArcBulgeFallback;



    [Tooltip("正弦：垂直于弦的最大偏移（世界单位）")]

    public float sineAmplitude = PathMovementLegDefaults.SineAmplitudeFallback;



    [Tooltip("正弦：振荡频率（Hz）")]

    public float sineHz = PathMovementLegDefaults.SineHzFallback;



    [Tooltip("正弦：初相（弧度）")]

    public float sinePhaseRad;

}



/// <summary>

/// 敌人运动路径：生成点为局部原点；nodes 为途经/终点（至少 1 个路径点）。

/// </summary>

[Serializable]

public class PathRouteMovementData

{

    [Tooltip("整条路径持续（秒）；<0 表示由路段自动合计")]

    public float durationSeconds = -1f;



    [NonSerialized] public int durationFrames = -1;



    [Tooltip("移动速度（世界单位/秒）；路段未指定 travelSeconds 时用于推算时长")]

    public float moveSpeed = PathMovementLegDefaults.MoveSpeedPerSecond;



    [NonSerialized] public float moveSpeedPerFrame;



    [Min(0f)]

    [Tooltip("在生成点停留的时间（秒），之后向首个路径点移动")]

    public float spawnHoldSeconds;



    [Tooltip("路径点（相对生成点）；至少 1 个。不含生成点本身")]

    public List<MovementPathNode> nodes = new()

    {

        new MovementPathNode { positionLocal = PathMovementLegDefaults.FallbackSegmentEnd }

    };



    [Tooltip("驶向各路径点的路段；legs[i] 为「上一位置 → nodes[i]」，数量建议与 nodes 相同")]

    public List<MovementPathLeg> legs = new();



    [Min(0f)]

    [Tooltip("未单独配置路段行驶时间时的默认段时长（秒）")]

    public float defaultLegDurationSeconds = PathMovementLegDefaults.DefaultLegDurationSeconds;



    public void BakeMovementTiming(uint logicFps)

    {

        float fps = Mathf.Max(1f, logicFps);

        moveSpeedPerFrame = moveSpeed / fps;

        if (durationSeconds < 0f)

            durationFrames = -1;

        else

            durationFrames = Mathf.Max(0, Mathf.RoundToInt(durationSeconds * fps));

    }



    /// <summary>移除旧版「起点」节点（局部原点），起点由生成点担任。</summary>

    public void EnsureSpawnAnchoredFormat()

    {

        if (nodes == null || nodes.Count < 2)

            return;



        if (nodes[0].positionLocal.sqrMagnitude > 1e-8f)

            return;



        float legacyHold = nodes[0].holdSeconds;

        if (legacyHold > 0.01f && spawnHoldSeconds < 0.01f)

            spawnHoldSeconds = legacyHold;



        nodes.RemoveAt(0);

    }



    public static bool HasAnyPathContent(PathRouteMovementData route)

    {

        if (route == null)

            return false;

        route.EnsureSpawnAnchoredFormat();

        return route.spawnHoldSeconds > 0.01f

               || (route.nodes != null && route.nodes.Count >= 1);

    }

    /// <summary>
    /// PerQueueEntry：条目仅改节点时，用波次 <paramref name="shared"/> 的路段曲线/时长配置补全 legs（不修改原资产）。
    /// </summary>
    public static PathRouteMovementData MergeLegsFromSharedFallback(
        PathRouteMovementData entry,
        PathRouteMovementData shared)
    {
        entry?.EnsureSpawnAnchoredFormat();
        shared?.EnsureSpawnAnchoredFormat();
        if (!ShouldMergeLegsFromShared(entry, shared))
            return entry;

        int nodeCount = entry.nodes.Count;
        var merged = new PathRouteMovementData
        {
            durationSeconds = entry.durationSeconds,
            moveSpeed = entry.moveSpeed,
            spawnHoldSeconds = entry.spawnHoldSeconds,
            defaultLegDurationSeconds = entry.defaultLegDurationSeconds > 0.01f
                ? entry.defaultLegDurationSeconds
                : shared.defaultLegDurationSeconds,
            nodes = CloneNodes(entry.nodes),
            legs = new List<MovementPathLeg>(nodeCount)
        };

        for (int i = 0; i < nodeCount; i++)
        {
            if (entry.legs != null && i < entry.legs.Count)
                merged.legs.Add(CloneLeg(entry.legs[i]));
            else if (shared.legs != null && i < shared.legs.Count)
                merged.legs.Add(CloneLeg(shared.legs[i]));
            else
                merged.legs.Add(new MovementPathLeg());
        }

        return merged;
    }

    public static bool ShouldMergeLegsFromShared(PathRouteMovementData entry, PathRouteMovementData shared)
    {
        if (entry?.nodes == null || entry.nodes.Count < 1)
            return false;
        if (shared?.legs == null || shared.legs.Count < 1)
            return false;
        return entry.legs == null || entry.legs.Count < entry.nodes.Count;
    }

    /// <summary>深拷贝路径配置（编辑器为条目创建 override 时使用）。</summary>
    public static PathRouteMovementData Duplicate(PathRouteMovementData source)
    {
        if (source == null)
            return null;

        source.EnsureSpawnAnchoredFormat();
        var dup = new PathRouteMovementData
        {
            durationSeconds = source.durationSeconds,
            moveSpeed = source.moveSpeed,
            spawnHoldSeconds = source.spawnHoldSeconds,
            defaultLegDurationSeconds = source.defaultLegDurationSeconds
        };

        if (source.nodes != null && source.nodes.Count > 0)
            dup.nodes = CloneNodes(source.nodes);

        if (source.legs != null && source.legs.Count > 0)
        {
            dup.legs = new List<MovementPathLeg>(source.legs.Count);
            for (int i = 0; i < source.legs.Count; i++)
                dup.legs.Add(CloneLeg(source.legs[i]));
        }

        return dup;
    }

    static List<MovementPathNode> CloneNodes(List<MovementPathNode> source)
    {
        var list = new List<MovementPathNode>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            var n = source[i];
            list.Add(new MovementPathNode
            {
                positionLocal = n.positionLocal,
                holdSeconds = n.holdSeconds
            });
        }

        return list;
    }

    static MovementPathLeg CloneLeg(MovementPathLeg leg) => new()
    {
        curve = leg.curve,
        travelSeconds = leg.travelSeconds,
        bezierHandle1Local = leg.bezierHandle1Local,
        bezierHandle2Local = leg.bezierHandle2Local,
        arcBulge = leg.arcBulge,
        sineAmplitude = leg.sineAmplitude,
        sineHz = leg.sineHz,
        sinePhaseRad = leg.sinePhaseRad
    };



    /// <summary>原地停留在生成点。</summary>

    public static PathRouteMovementData CreateStatic(float holdSeconds = 3600f)

    {

        return new PathRouteMovementData

        {

            moveSpeed = 0f,

            spawnHoldSeconds = holdSeconds,

            nodes = new List<MovementPathNode>(),

            legs = new List<MovementPathLeg>()

        };

    }



    /// <summary>沿方向直线运动指定距离（局部坐标，从生成点出发）。</summary>

    public static PathRouteMovementData CreateLinear(Vector2 direction, float distance, float moveSpeed)

    {

        Vector2 dir = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector2.down;

        return new PathRouteMovementData

        {

            moveSpeed = moveSpeed,

            nodes = new List<MovementPathNode>

            {

                new() { positionLocal = dir * distance }

            },

            legs = new List<MovementPathLeg>

            {

                new() { curve = E_PathSegmentCurve.Linear, travelSeconds = 0f }

            }

        };

    }



    public static PathRouteMovementData CreateLinearDown(float distance, float moveSpeed) =>

        CreateLinear(Vector2.down, distance, moveSpeed);



    /// <summary>单段三次贝塞尔（P0=生成点，终点局部坐标 p3）。</summary>

    public static PathRouteMovementData CreateCubicBezier(

        Vector2 p1, Vector2 p2, Vector2 p3,

        float moveSpeed,

        float durationSeconds = -1f)

    {

        return new PathRouteMovementData

        {

            durationSeconds = durationSeconds,

            moveSpeed = moveSpeed,

            nodes = new List<MovementPathNode>

            {

                new() { positionLocal = p3 }

            },

            legs = new List<MovementPathLeg>

            {

                new()

                {

                    curve = E_PathSegmentCurve.Bezier,

                    travelSeconds = 0f,

                    bezierHandle1Local = p1,

                    bezierHandle2Local = p3 + p2 - p3

                }

            }

        };

    }



#if UNITY_EDITOR

    public void OnValidate() => EnsureSpawnAnchoredFormat();

#endif

}


