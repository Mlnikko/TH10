using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class MovementPatternData
{
    public enum E_PatternType
    {
        Static = 0,
        Linear = 1,
        Sine = 2,
        Circle = 3,
        Bezier = 4,
        WaypointPolyline = 5,
        PathRoute = 6
    }

    public E_PatternType type;

    [Tooltip("整条轨迹持续（秒）；<0 表示无限（烘焙后 durationFrames 为 -1）")]
    public float durationSeconds = -1f;

    [NonSerialized] public int durationFrames = -1;

    [Tooltip("移动速度（世界单位/秒），与 CharacterConfig.moveSpeed 同量级")]
    public float moveSpeed = 3.6f;

    [NonSerialized] public float moveSpeedPerFrame;

    [Tooltip("正弦：垂直于主运动方向的最大偏移（世界单位）")]
    public float amplitude = 1f;

    [Tooltip("正弦：振荡频率（Hz）")]
    public float oscillationHz = 1f;

    [NonSerialized] public float sineOmegaPerFrame;

    [Tooltip("直线/正弦主轴方向（未归一化也可，烘焙时会归一）")]
    public Vector2 direction = Vector2.down;

    [Tooltip("贝塞尔：相对出生点的 4 个控制点 P0..P3（P0 一般为 0,0，可在烘焙时自动补）")]
    public List<Vector2> bezierPoints = new();

    /// <summary>由关卡时间轴在 <see cref="GameResDB"/> 解析阶段调用。</summary>
    public virtual void BakeMovementTiming(uint logicFps)
    {
        if (durationSeconds < 0f)
            durationFrames = -1;
        else
            durationFrames = Mathf.Max(0, Mathf.RoundToInt(durationSeconds * logicFps));

        BakeKinematics(logicFps);
    }

    protected void BakeKinematics(uint logicFps)
    {
        float fps = Mathf.Max(1f, logicFps);
        moveSpeedPerFrame = moveSpeed / fps;
        sineOmegaPerFrame = (Mathf.PI * 2f * oscillationHz) / fps;
    }
}

[Serializable]
public class StaticMovementData : MovementPatternData
{
    public StaticMovementData() => type = E_PatternType.Static;
}

[Serializable]
public class LinearMovementData : MovementPatternData
{
    public LinearMovementData() => type = E_PatternType.Linear;
}

[Serializable]
public class SineMovementData : MovementPatternData
{
    public SineMovementData() => type = E_PatternType.Sine;

    [Tooltip("相对主方向的垂直振动初相（弧度）")]
    public float phase0Rad;
}

[Serializable]
public class CircularMovementData : MovementPatternData
{
    public CircularMovementData() => type = E_PatternType.Circle;

    [Tooltip("轨道圆心相对出生点的偏移")]
    public Vector2 centerOffset;

    [Tooltip("轨道半径（世界单位）")]
    public float orbitRadius = 1.5f;

    [Tooltip("绕轨角速度（度/秒）")]
    public float angularSpeedDegPerSec = 90f;

    [NonSerialized] public float angularVelocityRadPerFrame;

    [Tooltip("起始角（度），相对 +X")]
    public float startAngleDeg;

    public override void BakeMovementTiming(uint logicFps)
    {
        base.BakeMovementTiming(logicFps);
        angularVelocityRadPerFrame = angularSpeedDegPerSec * Mathf.Deg2Rad / Mathf.Max(1f, logicFps);
    }
}

[Serializable]
public class BezierCubicMovementData : MovementPatternData
{
    public BezierCubicMovementData() => type = E_PatternType.Bezier;

    [Tooltip("若为空则用 bezierPoints；否则用本列表作为 P0..P3 局部控制点")]
    public List<Vector2> controlPointsLocal = new();
}

[Serializable]
public class WaypointPathMovementData : MovementPatternData
{
    public WaypointPathMovementData() => type = E_PatternType.WaypointPolyline;

    [Tooltip("相对出生点的路点（依次连接：出生点 → p0 → p1 → …）")]
    public List<Vector2> waypointsLocal = new();

    [Tooltip("每段路径持续时间（秒）；烘焙为 segmentFrames")]
    public List<float> segmentDurationSeconds = new();

    [NonSerialized] public List<int> segmentFrames = new();

    public override void BakeMovementTiming(uint logicFPS)
    {
        base.BakeMovementTiming(logicFPS);
        if (segmentFrames == null)
            segmentFrames = new List<int>();
        else
            segmentFrames.Clear();
        if (segmentDurationSeconds == null || segmentDurationSeconds.Count == 0)
            return;
        for (int i = 0; i < segmentDurationSeconds.Count; i++)
        {
            float s = segmentDurationSeconds[i];
            segmentFrames.Add(s <= 0f ? 1 : Mathf.Max(1, Mathf.RoundToInt(s * logicFPS)));
        }
    }
}

/// <summary>路径段曲线类型（相邻节点之间的运动方式）。</summary>
public enum E_PathSegmentCurve : byte
{
    Linear = 0,
    Arc = 1,
    Bezier = 2
}

[Serializable]
public class MovementPathNode
{
    [Tooltip("相对本波出生点的局部坐标")]
    public Vector2 positionLocal;

    [Min(0f)]
    [Tooltip("到达该节点后的停留时间（秒）")]
    public float holdSeconds;
}

[Serializable]
public class MovementPathLeg
{
    public E_PathSegmentCurve curve = E_PathSegmentCurve.Linear;

    [Min(0f)]
    [Tooltip("本段行驶时间（秒）；≤0 时用路径默认段时长或按 moveSpeed 推算")]
    public float travelSeconds;

    [Tooltip("贝塞尔：相对段起点的控制点 1")]
    public Vector2 bezierHandle1Local;

    [Tooltip("贝塞尔：相对段终点的控制点 2")]
    public Vector2 bezierHandle2Local;

    [Tooltip("圆弧：中段垂直于弦的拱高（世界单位，可正可负）")]
    public float arcBulge = 0.5f;
}

/// <summary>
/// 起终点 + 停留点路径；每段可选直线/圆弧/贝塞尔。节点[0] 为起点，最后一项为终点。
/// </summary>
[Serializable]
public class PathRouteMovementData : MovementPatternData
{
    public PathRouteMovementData() => type = E_PatternType.PathRoute;

    [Tooltip("路径节点（至少 2 个：起点 → … → 终点）")]
    public List<MovementPathNode> nodes = new()
    {
        new MovementPathNode { positionLocal = Vector2.zero },
        new MovementPathNode { positionLocal = new Vector2(0f, -3f) }
    };

    [Tooltip("相邻节点间的路段配置；数量应为 nodes.Count - 1，不足时按直线+默认时长补全")]
    public List<MovementPathLeg> legs = new();

    [Min(0f)]
    [Tooltip("未单独配置路段行驶时间时的默认段时长（秒）")]
    public float defaultLegDurationSeconds = 1f;
}
