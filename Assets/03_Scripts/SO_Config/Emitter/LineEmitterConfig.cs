using UnityEngine;

[CreateAssetMenu(fileName = "NewLineEmitterConfig", menuName = "DanmakuEmitter/LineEmitterConfig")]
public class LineEmitterConfig : DanmakuEmitterConfig
{
    [Header("œﬂ–Œ∑¢…‰∆˜≈‰÷√")]
    public Vector2 Direction;
    public float Speed;
    public int LineCount;
    public float LineSpace;
    
    public LineEmitterConfig() : base() 
    {
        EmitterType = E_EmitterType.Line;
        Direction = Vector2.up;
        Speed = 1;   
        LineCount = 1;
        LineSpace = 0.2f; 
    }
}
