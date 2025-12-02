using UnityEngine;

public enum EmitterCamp
{
    None,
    Player,
    Enemy
}

public class DanmakuEmitter : MonoBehaviour
{
    public DanmakuEmitterConfig EmitterConfig => emitterConfig;

    [Header("发射控制")]
    [SerializeField] protected bool fireable;
    public bool Fireable
    {
        get => fireable;
        set => fireable = value;
    }

    [Header("配置文件")]

    [SerializeField] protected DanmakuEmitterConfig emitterConfig;
    [SerializeField] protected DanmakuConfig danmakuConfig;

    [Header("对象池配置")]
    protected ObjectPool<DanmakuConfiger> danmakuPool;
    [SerializeField] protected int poolMinSize;
    [SerializeField] protected int poolMaxSize;

    [Header("弹幕初始化配置")]
    [SerializeField] protected Vector2 positionOffset;
    [SerializeField] protected Vector3 startRotation;
    [SerializeField] protected Vector2 startVelocity;

    [Header("音效设置")]
    [SerializeField] AudioName audio_Fire;

    [Header("发射器所属阵营")]
    [SerializeField] protected EmitterCamp emitterCamp;

    [Header("发射间隔")]
    [SerializeField] protected float launchInterval;

    [Header("发射速度")]
    [SerializeField] protected float launchSpeed;

    public void LoadEmitterConfig()
    {
        if (emitterConfig == null) return;

        poolMinSize = emitterConfig.MinSize;
        poolMaxSize = emitterConfig.MaxSize;

        positionOffset = emitterConfig.PositionOffset;
        startRotation = emitterConfig.StartRotation;
        startVelocity = emitterConfig.StartVelocity;

        launchInterval = emitterConfig.LaunchInterval;
        launchSpeed = emitterConfig.LaunchSpeed;

        audio_Fire = emitterConfig.Audio_Fire;
        emitterCamp = emitterConfig.EmitterCamp;

        GameLogger.Debug("已加载发射器配置" + emitterConfig.name);
    }
}
