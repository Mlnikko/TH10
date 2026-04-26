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
        Aimed = 6
    }

    public E_PatternType type;

    [Tooltip("整条轨迹持续（秒）；<0 表示无限（烘焙后 durationFrames 为 -1）")]
    public float durationSeconds = -1f;

    [NonSerialized] public int durationFrames = -1;

    /// <summary>由关卡时间轴在 <see cref="GameResDB"/> 解析阶段调用。</summary>
    public virtual void BakeMovementTiming(uint logicFps)
    {
        if (durationSeconds < 0f)
            durationFrames = -1;
        else
            durationFrames = Mathf.Max(0, Mathf.RoundToInt(durationSeconds * logicFps));
    }

    [Tooltip("直线/瞄准：沿 direction 的标量速度（世界单位 / 逻辑帧）")]
    public float speed = 0.12f;

    [Tooltip("正弦：垂直于主运动方向的最大偏移（世界单位）")]
    public float amplitude = 0.25f;

    [Tooltip("正弦：角速度（弧度 / 逻辑帧）")]
    public float frequency = 0.08f;

    [Tooltip("直线/正弦主轴方向（未归一化也可，烘焙时会归一）")]
    public Vector2 direction = Vector2.down;

    [Tooltip("贝塞尔：相对出生点的 4 个控制点 P0..P3（P0 一般为 0,0，可在烘焙时自动补）")]
    public List<Vector2> bezierPoints = new();
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
    public float radius = 1f;

    [Tooltip("角速度（弧度 / 逻辑帧），正值逆时针")]
    public float angularVelocityRadPerFrame = 0.02f;

    [Tooltip("起始角（度），相对 +X")]
    public float startAngleDeg;
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

[Serializable]
public class AimedLinearMovementData : MovementPatternData
{
    public AimedLinearMovementData() => type = E_PatternType.Aimed;
}
