using UnityEngine;

[CreateAssetMenu(fileName = "NewArcEmitterConfig", menuName = "DanmakuEmitter/ArcEmitterConfig")]
public class ArcEmitterConfig : DanmakuEmitterConfig
{
    [Header("ª∑–Œ∑¢…‰∆˜≈‰÷√")]
    public int DirectionCount;
    public float StartAngle;
    public float EndAngle;
    public bool UseRelativeAngle;

    public ArcEmitterConfig() : base()
    {
        DirectionCount = 8;
        StartAngle = 0.0f;
        EndAngle = 360.0f;
        UseRelativeAngle = true;
    }
}
