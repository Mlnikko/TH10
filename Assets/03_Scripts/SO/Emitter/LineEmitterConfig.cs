using UnityEngine;

[CreateAssetMenu(fileName = "NewLineEmitterConfig", menuName = "DanmakuEmitter/LineEmitterConfig")]
public class LineEmitterConfig : DanmakuEmitterConfig
{
    [Header("œﬂ–Œ∑¢…‰∆˜≈‰÷√")]

    [Range(-1, 1)]
    public float DirX;
    [Range(-1, 1)]
    public float DirY;

    public float Speed;
    public int LineCount;
    public float LineSpace;
    
    public LineEmitterConfig() : base() 
    {
        DirX = 0;
        DirY = 1;
        Speed = 1;   
        LineCount = 1;
        LineSpace = 0.2f; 
    }
}
