using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DanmakuConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => danmakuConfig != null;

    public DanmakuConfig danmakuConfig;

    [SerializeField] E_DanmakuType danmakuType;

    [Header("弹幕缩放")]
    [SerializeField] float scale;

    [Header("弹幕渲染设置")]
    [SerializeField] Sprite sprite;
    [SerializeField] Color color;

    [Header("弹幕碰撞器设置")]
    [SerializeField] ColliderConfig colliderConfig;

    [SerializeField] float damage;

    public void LoadDanmakuConfig() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (danmakuConfig == null)
        {
            Logger.Warn("弹幕配置文件未设置", LogTag.Config);
            return;
        }
        scale = danmakuConfig.scale;

        sprite = danmakuConfig.sprite;
        color = danmakuConfig.color;
       
        danmakuType = danmakuConfig.danmakuType;

        colliderConfig = danmakuConfig.colliderConfig;

        damage = danmakuConfig.damage;

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

        danmakuConfig.sprite = sprite;
        danmakuConfig.color = color;
       
        danmakuConfig.danmakuType = danmakuType;

        danmakuConfig.colliderConfig = colliderConfig;

        danmakuConfig.damage = damage;

        Logger.Debug($"弹幕配置文件保存完成: {danmakuConfig.name}");
    }

    protected override void ApplyEditorPreview() => ApplyDanmakuVisual();

    void ApplyDanmakuVisual()
    {
        ConfigViewerSpritePreview.Apply(transform, sprite, color, scale);
    }

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
