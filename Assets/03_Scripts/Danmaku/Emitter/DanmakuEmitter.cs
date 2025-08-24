using System;
using UnityEngine;
public abstract class DanmakuEmitter : MonoBehaviour
{
    [SerializeField] protected DanmakuEmitterConfig emitterConfig;

    [SerializeField] GameObject danmakuPrefab;
    DanmakuConfig danmakuConfig;

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
    public E_EmitterType emitterType;

    void Awake()
    {
        LoadDanmakuConfig();
        LoadEmitterConfig();
        InitDanmakuPool();
    }

    void LoadDanmakuConfig()
    {
        if (danmakuPrefab == null) return;
        danmakuConfig = danmakuPrefab.GetComponent<DanmakuPrefab>().DanmakuConfig;
        GameLogger.Debug("已加载发射器弹幕配置" + danmakuConfig.name);
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

        OnEmitterConfigLoad();

        GameLogger.Debug("已加载发射器配置" + emitterConfig.name);
    }

    protected abstract void OnEmitterConfigLoad();

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

        OnEmitterConfigSave();

        GameLogger.Debug("已保存发射器配置" + emitterConfig.name);
    }

    protected abstract void OnEmitterConfigSave();

    void InitDanmakuPool()
    {
        danmakuPool = new();
        danmakuPool.InitPool(CreateDanmaku, poolMinSize, poolMaxSize);
        danmakuPool.OnGet += OnDanmakuGet;
        danmakuPool.OnRelease += OnDanmakuRelease;
    }

    Danmaku CreateDanmaku()
    {
        GameObject go = Instantiate(danmakuPrefab, transform);
        go.SetActive(false);
        DanmakuEntity entity = DanmakuEntityManager.CreateCustomDanmakuEntity(danmakuConfig, emitterConfig);
        Danmaku danmaku = new(go, entity, danmakuPool);
        return danmaku;
    }

    protected virtual void OnDanmakuGet(Danmaku danmaku)
    {
        danmaku.GameObject.SetActive(true);
    }
    protected virtual void OnDanmakuRelease(Danmaku obj)
    {
        obj.GameObject.SetActive(false);
    }

    void Update()
    {
        if (!fireable || !emitterConfig || !danmakuConfig) return;

        emitterTimer -= Time.deltaTime;
        if (emitterTimer < 0)
        {
            FireDanmaku();
            emitterTimer = launchInterval;
        }
    }

    public void SetEmitterFireable(bool fireable)
    {
        emitterTimer = 0;
        this.fireable = fireable;
    }

    protected abstract void FireDanmaku();
}
