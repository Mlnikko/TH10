using UnityEngine;

public enum EmitMode
{ 
    None,
    Line,
    Arc
}

public enum DanmakuSelectMode
{
    First,
    Sequential,
    Random
}


[CreateAssetMenu(fileName = "NewDanmakuEmitterConfig", menuName = "Configs/DanmakuEmitterConfig")]
public class DanmakuEmitterConfig : GameConfig
{
    public string[] danmakuConfigIds;
    public DanmakuSelectMode danmakuSelectMode = DanmakuSelectMode.First;

    public EmitMode emitMode = EmitMode.None;

    //public int minPoolSize = 100;
    //public int maxPoolSize = 500;

    [Header("通用发射器参数")]

    [Min(0f)] public float launchInterval = 0.5f;
    public float launchSpeed = 2f;

    public Vector2 launchPosOffset = Vector2.zero;
    public Vector3 launchRotOffset = Vector3.zero;

    public EmitterCamp emitterCamp = EmitterCamp.Enemy;
    public AudioName audio_Fire = AudioName.None;

    [Header("Line 发射器")]

    [Tooltip("发射方向，会转换成单位向量使用")]
    public Vector2 LineDirection = Vector2.up; // 替代 DirX/DirY
    [Min(1)] public int LineCount = 1;
    [Min(0f)] public float LineSpacing = 0.2f;


    [Header("Arc 发射器")]

    public float ArcAngle = 90f;      // 弧度范围（度）

    [Min(0f)]
    public int ArcBulletCount = 5;    // 弧线上子弹数
    public bool ArcClockwise = true;
}
