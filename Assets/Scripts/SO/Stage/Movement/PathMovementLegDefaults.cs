using UnityEngine;

/// <summary>
/// 路径段曲线默认参数：对齐道中 <see cref="EnemyWaveConfig.defaultDescentSpeed"/>（3.6 世界单位/秒）
/// 与战斗区外侧带尺度（约 100 单位高），贴近东方 STG 妖精弧线/蛇行观感。
/// </summary>
public static class PathMovementLegDefaults
{
    /// <summary>与 <see cref="EnemyWaveConfig.defaultDescentSpeed"/>、<see cref="PathRouteMovementData.moveSpeed"/> 一致。</summary>
    public const float MoveSpeedPerSecond = 3.6f;

    /// <summary>未指定 <see cref="MovementPathLeg.travelSeconds"/> 时的默认段时长（秒）。</summary>
    public const float DefaultLegDurationSeconds = 1.5f;

    /// <summary>无弦长信息时的典型首段下移（世界单位）。</summary>
    public static readonly Vector2 FallbackSegmentEnd = new(0f, -2.5f);

    public const float ArcBulgeFallback = 1.2f;
    public const float SineAmplitudeFallback = 0.65f;
    public const float SineHzFallback = 1.2f;

    /// <summary>将 <paramref name="leg"/> 设为对应曲线类型的推荐初值（可按路段弦长缩放）。</summary>
    public static void Apply(MovementPathLeg leg, E_PathSegmentCurve curve, Vector2 segmentFrom, Vector2 segmentTo)
    {
        if (leg == null)
            return;

        leg.curve = curve;
        leg.travelSeconds = 0f;

        Vector2 chord = segmentTo - segmentFrom;
        float chordLen = chord.magnitude;
        if (chordLen < 1e-4f)
            chord = FallbackSegmentEnd;
        chordLen = chord.magnitude;

        Vector2 dir = chord / chordLen;
        Vector2 perp = new(-dir.y, dir.x);

        switch (curve)
        {
            case E_PathSegmentCurve.Arc:
                leg.arcBulge = Mathf.Clamp(chordLen * 0.42f, 0.6f, 2.2f);
                break;

            case E_PathSegmentCurve.Bezier:
                leg.bezierHandle1Local = dir * (chordLen * 0.33f) + perp * (chordLen * 0.18f);
                leg.bezierHandle2Local = -dir * (chordLen * 0.33f) - perp * (chordLen * 0.14f);
                break;

            case E_PathSegmentCurve.Sine:
                leg.sineAmplitude = Mathf.Clamp(chordLen * 0.22f, 0.35f, 1.1f);
                leg.sineHz = SineHzFallback;
                leg.sinePhaseRad = 0f;
                break;
        }
    }

    public static void Apply(MovementPathLeg leg, E_PathSegmentCurve curve) =>
        Apply(leg, curve, Vector2.zero, FallbackSegmentEnd);
}
