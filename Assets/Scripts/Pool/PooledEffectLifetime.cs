using UnityEngine;

/// <summary>
/// 池化<strong>纯粒子</strong>特效：OnGet 播放子节点 <see cref="ParticleSystem"/>，结束后回收到 <see cref="GameObjectPoolManager"/>。
/// 挂到 <c>Assets/Prefabs/Effect/</c> 根节点；勿使用 Animator（死亡特效与小怪击杀数量多，粒子更合适）。
/// </summary>
[DisallowMultipleComponent]
public class PooledEffectLifetime : MonoBehaviour, IPoolable
{
    [SerializeField]
    [Tooltip("无粒子或循环粒子时使用的回池时间（秒，unscaled）")]
    float fallbackLifetimeSeconds = 0.6f;

    [SerializeField]
    [Tooltip("根据各 ParticleSystem 的 duration + startLifetime 估算播放时长并回池")]
    bool deriveLifetimeFromParticles = true;

    [SerializeField]
    [Tooltip("在估算时长上额外增加的缓冲（秒），避免粒子未播完就回收")]
    float lifetimePaddingSeconds = 0.1f;

    ParticleSystem[] _particleSystems;
    float _returnAtUnscaledTime = -1f;

    void Awake() => CacheParticleSystems();

#if UNITY_EDITOR
    void OnValidate() => CacheParticleSystems();
#endif

    void CacheParticleSystems()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    /// <summary>
    /// 在 <see cref="GameObject.SetActive"/> 为 true 之后调用，确保粒子播放与回池计时。
    /// 池内物体在 Get 时可能仍为 inactive，故不可仅依赖 <see cref="GameObjectPoolManager"/> 内的 OnGet。
    /// </summary>
    public static void ActivateAfterSpawn(GameObject go)
    {
        if (go == null)
            return;

        var lifetime = go.GetComponent<PooledEffectLifetime>();
        if (lifetime == null)
            lifetime = go.AddComponent<PooledEffectLifetime>();

        lifetime.OnGet();
    }

    public void OnGet()
    {
        CacheParticleSystems();

        float lifetime = ResolveLifetimeSeconds();
        _returnAtUnscaledTime = Time.unscaledTime + lifetime;

        if (_particleSystems == null || _particleSystems.Length == 0)
        {
#if UNITY_EDITOR
            Logger.Warn($"[PooledEffectLifetime] No ParticleSystem on '{name}'. Using fallback lifetime.", LogTag.Pool);
#endif
            return;
        }

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            var ps = _particleSystems[i];
            if (ps == null)
                continue;

            ps.Clear(true);
            ps.Play(true);
        }
    }

    public void OnReturn()
    {
        _returnAtUnscaledTime = -1f;

        if (_particleSystems == null)
            return;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            var ps = _particleSystems[i];
            if (ps == null)
                continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    float ResolveLifetimeSeconds()
    {
        if (!deriveLifetimeFromParticles || _particleSystems == null || _particleSystems.Length == 0)
            return Mathf.Max(0.05f, fallbackLifetimeSeconds);

        float maxEstimate = 0f;
        bool anyNonLoop = false;

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            var ps = _particleSystems[i];
            if (ps == null)
                continue;

            float estimate = EstimateParticlePlaySeconds(ps);
            if (estimate < 0f)
                continue;

            anyNonLoop = true;
            if (estimate > maxEstimate)
                maxEstimate = estimate;
        }

        if (!anyNonLoop)
            return Mathf.Max(0.05f, fallbackLifetimeSeconds);

        return Mathf.Max(0.05f, maxEstimate + lifetimePaddingSeconds);
    }

    /// <summary>非循环粒子：duration + 最大起始寿命；循环粒子返回 -1 表示走 fallback。</summary>
    static float EstimateParticlePlaySeconds(ParticleSystem ps)
    {
        var main = ps.main;
        if (main.loop)
            return -1f;

        float startLifeMax = main.startLifetime.mode switch
        {
            ParticleSystemCurveMode.Constant => main.startLifetime.constant,
            ParticleSystemCurveMode.TwoConstants => main.startLifetime.constantMax,
            _ => main.startLifetime.constantMax,
        };

        return main.duration + Mathf.Max(0f, startLifeMax);
    }

    void Update()
    {
        if (_returnAtUnscaledTime < 0f)
            return;

        if (Time.unscaledTime < _returnAtUnscaledTime)
            return;

        _returnAtUnscaledTime = -1f;
        if (GameObjectPoolManager.Instance != null)
            GameObjectPoolManager.Instance.Return(gameObject);
        else
            gameObject.SetActive(false);
    }
}
