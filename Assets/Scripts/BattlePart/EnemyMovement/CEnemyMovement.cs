/// <summary>
/// 敌人位移模式（烘焙自 <see cref="MovementPatternData"/>）。
/// </summary>
public enum E_EnemyMotionKind : byte
{
    None = 0,
    Static = 1,
    Linear = 2,
    Sine = 3,
    Orbit = 4,
    CubicBezier = 5,
    WaypointPolyline = 6,
    AimedLinear = 7
}

/// <summary>
/// 逻辑帧上驱动的敌人运动状态（紧凑布局，由 <see cref="EnemyMovementBaking"/> 写入）。
/// </summary>
public struct CEnemyMovement : IComponent
{
    public E_EnemyMotionKind kind;
    public uint spawnFrame;
    public float originX, originY;
    /// <summary>-1 表示无限持续时间。</summary>
    public int durationFrames;

    public float dX, dY;

    public float perpX, perpY, sineAmp, sineOmega, sinePhase0;

    public float orbitCx, orbitCy, orbitR, orbitOmega, orbitPhase0;

    public float b1x, b1y, b2x, b2y, b3x, b3y;

    public byte wayCount;
    public float wp1x, wp1y, wp2x, wp2y, wp3x, wp3y, wp4x, wp4y;
    public int segEnd0, segEnd1, segEnd2, segEnd3;
}
