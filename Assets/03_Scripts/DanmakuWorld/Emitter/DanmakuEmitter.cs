using UnityEngine;

public abstract class DanmakuEmitter : MonoBehaviour
{
    [SerializeField] protected DanmakuEmitterConfig emitterConfig;
    [SerializeField] GameObject danmakuPrefab;
    [SerializeField] Transform poolRoot;

    [Header("对象池配置")]
    protected ObjectPool<Danmaku> danmakuPool;
    [SerializeField] protected int poolMinSize;
    [SerializeField] protected int poolMaxSize;

    [Header("发射控制")]
    [SerializeField] protected bool fireable;

    [Header("弹幕初始化配置")]
    [SerializeField] protected Vector2 positionOffset;
    [SerializeField] protected Vector3 startRotation;
    [SerializeField] protected Vector2 startVelocity;

    [Header("音效设置")]
    [SerializeField] E_AudioName audio_Fire;

    protected Vector3 FirePosition
    {
        get
        {
            return transform.position + (Vector3)positionOffset;
        }
    }

    [Header("发射间隔")]
    [SerializeField] protected float launchInterval;

    [Header("发射速度")]
    [SerializeField] protected float launchSpeed;
    float emitterTimer;

    [Header("发射器类型")]
    [SerializeField] protected E_EmitterType emitterType;

    [Header("弹幕所属阵营")]
    [SerializeField] protected E_DanmakuCamp danmakuCamp;

    void Awake()
    {
        LoadDanmakuConfig();
        LoadEmitterConfig();
        InitDanmakuPool();
    }

    void LoadDanmakuConfig()
    {
        if (danmakuPrefab == null) return;
    }
    public void LoadEmitterConfig()
    {
        if (emitterConfig == null) return;

        poolMinSize = emitterConfig.MinSize;
        poolMaxSize = emitterConfig.MaxSize;

        fireable = emitterConfig.Fireable;

        positionOffset = emitterConfig.PositionOffset;
        startRotation = emitterConfig.StartRotation;
        startVelocity = emitterConfig.StartVelocity;

        launchInterval = emitterConfig.LaunchInterval;
        launchSpeed = emitterConfig.LaunchSpeed;

        emitterType = emitterConfig.EmitterType;

        danmakuCamp = emitterConfig.DanmakuCamp;

        audio_Fire = emitterConfig.Audio_Fire;

        OnEmitterConfigLoad();

        GameLogger.Debug("已加载发射器配置" + emitterConfig.name);
    }

    protected abstract void OnEmitterConfigLoad();

#if UNITY_EDITOR
    public void PreviewEmitterEffect()
    {
        if (emitterConfig == null) return;

        // TODO: 编辑器下可预览发射效果
    }
    public void SaveEmitterConfig()
    {
        if (emitterConfig == null) return;

        emitterConfig.MinSize = poolMinSize;
        emitterConfig.MaxSize = poolMaxSize;

        emitterConfig.Fireable = fireable;

        emitterConfig.PositionOffset = positionOffset;
        emitterConfig.StartVelocity = startVelocity;
        emitterConfig.StartRotation = startRotation;

        emitterConfig.LaunchInterval = launchInterval;
        emitterConfig.LaunchSpeed = launchSpeed;

        emitterConfig.EmitterType = emitterType;

        emitterConfig.DanmakuCamp = danmakuCamp;

        emitterConfig.Audio_Fire = audio_Fire;

        OnEmitterConfigSave();

        UnityEditor.EditorUtility.SetDirty(emitterConfig);
        UnityEditor.AssetDatabase.SaveAssets();

        GameLogger.Debug("已保存发射器配置" + emitterConfig.name);
    }
    protected abstract void OnEmitterConfigSave();
#endif

    void InitDanmakuPool()
    {
        danmakuPool = new();
        danmakuPool.InitPool(CreateDanmaku, poolMinSize, poolMaxSize);
        danmakuPool.OnGet += OnDanmakuGet;
        danmakuPool.OnRelease += OnDanmakuRelease;
    }

    Danmaku CreateDanmaku()
    {
        Danmaku danmaku = Instantiate(danmakuPrefab, poolRoot).GetComponent<Danmaku>();
        danmaku.InitDanmaku(danmakuPool, danmakuCamp);
        return danmaku;
    }

    protected virtual void OnDanmakuGet(Danmaku danmaku)
    {
        danmaku.SetActive(true);
    }
    protected virtual void OnDanmakuRelease(Danmaku danmaku)
    {
        danmaku.SetActive(false);
    }

    void Update()
    {
        if (!fireable || !emitterConfig) return;

        emitterTimer -= Time.deltaTime;
        if (emitterTimer < 0)
        {
            OnDanmakuFire();
            emitterTimer = launchInterval;
        }
    }

    public void SetFireable(bool value)
    {
        fireable = value;
        if(!fireable)
        {
            OnDanmakuStopFire();
        }
    }

    protected virtual void OnDanmakuFire()
    {
        AudioManager.Instance.PlayAudio(emitterConfig.Audio_Fire);
    }

    protected virtual void OnDanmakuStopFire()
    {
        AudioManager.Instance.StopAudio(emitterConfig.Audio_Fire);
    }
}
