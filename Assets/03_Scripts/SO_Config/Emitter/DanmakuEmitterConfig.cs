using UnityEngine;
public enum E_EmitterType
{ 
    None,
    Line,
    Arc
}

public class DanmakuEmitterConfig : ScriptableObject
{
    [Header("对象池设置")]
    public int MinSize;
    public int MaxSize;

    [Header("发射控制")]
    public bool Fireable;

    public Vector2 PositionOffset;
    public Vector3 StartRotation;
    public Vector2 StartVelocity;

    [Header("发射间隔")]
    public float LaunchInterval;

    [Header("发射速度")]
    public float LaunchSpeed;

    [Header("发射器类型")]
    public E_EmitterType EmitterType;

    [Header("弹幕所属阵营")]
    public E_DanmakuCamp DanmakuCamp;

    [Header("音效设置")]
    public E_AudioName Audio_Fire;

    public DanmakuEmitterConfig()
    {
        MinSize = 20;
        MaxSize = 500;
        Fireable = false;
        PositionOffset = Vector2.zero;
        StartRotation = Vector3.zero;
        StartVelocity = Vector2.zero;
        LaunchInterval = 0.5f;
        LaunchSpeed = 2f;
        EmitterType = E_EmitterType.None;
        DanmakuCamp = E_DanmakuCamp.None;
    }
}
