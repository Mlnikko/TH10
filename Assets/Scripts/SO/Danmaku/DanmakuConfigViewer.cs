using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
/// <summary>弹幕预制体配置编辑；不参与运行时逻辑。</summary>
public class DanmakuConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => danmakuConfig != null;

    public DanmakuConfig danmakuConfig;

    [Header("弹幕预制体")]
    [SerializeField] string danmakuPrefabId;

    [SerializeField] E_DanmakuType danmakuType;

    [Header("弹幕缩放")]
    [SerializeField] float scale;

    [Header("弹幕渲染设置")]
    [SerializeField] Sprite sprite;

    [Header("弹幕碰撞器设置")]
    [SerializeField] ColliderConfig colliderConfig;

    [SerializeField] float damage;

    [Header("命中表现")]
    [PoolPrefabId(E_PoolCategory.Effect)]
    [SerializeField] string hitEffectPrefabId;

    [Header("追踪弹幕（外弧转向）")]
    [SerializeField] E_ColliderLayer homingTargetLayers = E_ColliderLayer.Enemy;
    [SerializeField] float homingTurnSpeedDegreesPerSecond = 420f;

    public void LoadDanmakuConfig() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (danmakuConfig == null)
        {
            Logger.Warn("弹幕配置文件未设置", LogTag.Config);
            return;
        }
        scale = danmakuConfig.scale;

        danmakuPrefabId = danmakuConfig.danmakuPrefabId;
        sprite = danmakuConfig.sprite;

        danmakuType = danmakuConfig.danmakuType;

        colliderConfig = danmakuConfig.colliderConfig;

        damage = danmakuConfig.damage;
        hitEffectPrefabId = danmakuConfig.hitEffectPrefabId;

        homingTargetLayers = danmakuConfig.homingTargetLayers;
        homingTurnSpeedDegreesPerSecond = danmakuConfig.homingTurnSpeedDegreesPerSecond;

        Logger.Debug($"弹幕配置文件加载完成: {danmakuConfig.name}");
    }

    public void SaveDanmakuConfig()
    {
        if (danmakuConfig == null)
        {
            Logger.Warn("弹幕配置文件未设置", LogTag.Config);
            return;
        }
        danmakuConfig.scale = scale;

        danmakuConfig.danmakuPrefabId = danmakuPrefabId;
        danmakuConfig.sprite = sprite;

        danmakuConfig.danmakuType = danmakuType;

        danmakuConfig.colliderConfig = colliderConfig;

        danmakuConfig.damage = damage;
        danmakuConfig.hitEffectPrefabId = hitEffectPrefabId;

        if (danmakuType == E_DanmakuType.Homing)
        {
            danmakuConfig.homingTargetLayers = homingTargetLayers;
            danmakuConfig.homingTurnSpeedDegreesPerSecond = homingTurnSpeedDegreesPerSecond;
        }

        Logger.Debug($"弹幕配置文件保存完成: {danmakuConfig.name}");
    }

    protected override void ApplyEditorPreview() => ApplyDanmakuVisual();

    void ApplyDanmakuVisual() =>
        DanmakuPresentation.Apply(sprite, scale, gameObject);

    public void PreviewDanmaku()
    {
        LoadFromConfig();
        ApplyDanmakuVisual();
    }

    protected void OnDrawGizmosSelected()
    {      
        if (danmakuConfig == null) return;
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, colliderConfig, Color.yellow, Color.green);
    }
}
