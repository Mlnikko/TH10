using UnityEngine;

public enum EmitterType
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

    //[Header("发射控制")]
    //public bool Fireable;

    public Vector2 PositionOffset;
    public Vector3 StartRotation;
    public Vector2 StartVelocity;

    [Header("发射间隔")]
    public float LaunchInterval;

    [Header("发射速度")]
    public float LaunchSpeed;

    [Header("音效设置")]
    public AudioName Audio_Fire;

    [Header("发射器阵营")]
    public EmitterCamp EmitterCamp;

    public DanmakuEmitterConfig()
    {
        MinSize = 20;
        MaxSize = 500;
        //Fireable = false;
        PositionOffset = Vector2.zero;
        StartRotation = Vector3.zero;
        StartVelocity = Vector2.zero;
        LaunchInterval = 0.5f;
        LaunchSpeed = 2f;
        Audio_Fire = AudioName.None;
        EmitterCamp = EmitterCamp.None;
    }
}
