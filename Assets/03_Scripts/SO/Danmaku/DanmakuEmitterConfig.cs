using UnityEngine;

public enum EmitterType
{ 
    None,
    Line,
    Arc
}

[CreateAssetMenu(fileName = "NewDanmakuEmitterConfig", menuName = "Configs/DanmakuEmitterPrefabTool")]
public class DanmakuEmitterConfig : GameConfig
{
    public EmitterType Type = EmitterType.None;
    public int MinPoolSize = 100;
    public int MaxPoolSize = 500;
    public float LaunchInterval = 0.5f;
    public float LaunchSpeed = 2f;
    public Vector2 PositionOffset = Vector2.zero;
    public Vector3 StartRotation = Vector3.zero;
    public EmitterCamp EmitterCamp = EmitterCamp.Enemy;
    public AudioName Audio_Fire = AudioName.None;

    // === Line 发射器参数（仅当 Type == Line 时有效）===
    [Header("Line 发射器")]
    public Vector2 LineDirection = Vector2.up; // 替代 DirX/DirY
    public int LineCount = 1;
    public float LineSpacing = 0.2f;

    // === Arc 发射器参数（仅当 Type == Arc 时有效）===
    [Header("Arc 发射器")]
    public float ArcAngle = 90f;      // 弧度范围（度）
    public int ArcBulletCount = 5;    // 弧线上子弹数
    public bool ArcClockwise = true;

    public override string AddressableKeyPrefix => ConfigHelper.DANMAKU_EMITTER_CONFIG_PREFIX;
}
