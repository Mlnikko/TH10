using UnityEngine;

[RequireComponent (typeof(SpriteRenderer))]
public class DanmakuPrefabTool : MonoBehaviour
{
    public DanmakuConfig danmakuConfig;

    [SerializeField] DanmakuType danmakuType;

    [Header("弹幕预制体缩放设置")]
    [SerializeField] Vector2 localScale;

    [Header("弹幕渲染设置")]
    [SerializeField] Sprite sprite;
    [SerializeField] Color color;

    [Header("弹幕碰撞器设置")]
    [SerializeField] Vector2 colliderOffset;
    [SerializeField] E_ColliderType colliderType;
    [SerializeField] Vector2 size;
    [SerializeField] float radius;

    [SerializeField] float damage;

    public void LoadDanmakuConfig()
    {
        if (danmakuConfig == null)
        {
            Logger.Warn("弹幕配置文件未设置", LogTag.Config);
            return;
        }

        localScale = danmakuConfig.LocalScale;
        sprite = danmakuConfig.Sprite;
        color = danmakuConfig.Color;
        colliderOffset = danmakuConfig.ColliderOffset;
        colliderType = danmakuConfig.ColliderType;
        size = danmakuConfig.Size;
        radius = danmakuConfig.Radius;
        danmakuType = danmakuConfig.DanmakuType;
        damage = danmakuConfig.Damage;

        Logger.Debug($"弹幕配置文件加载完成: {danmakuConfig.name}");
    }

    public void SaveDanmakuConfig()
    {
        if (danmakuConfig == null)
        {
            Logger.Warn("弹幕配置文件未设置", LogTag.Config);
            return;
        }

        danmakuConfig.LocalScale = localScale;
        danmakuConfig.Sprite = sprite;
        danmakuConfig.Color = color;
        danmakuConfig.ColliderOffset = colliderOffset;
        danmakuConfig.ColliderType = colliderType;
        danmakuConfig.Size = size;
        danmakuConfig.Radius = radius;
        danmakuConfig.DanmakuType = danmakuType;
        danmakuConfig.Damage = damage;

        Logger.Debug($"弹幕配置文件保存完成: {danmakuConfig.name}");
    }

    public void PreviewDanmaku()
    {
        LoadDanmakuConfig();

        // 预览缩放
        transform.localScale = localScale;

        // 预览渲染
        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
        }
    }

    protected void OnDrawGizmosSelected()
    {
        
        if (danmakuConfig == null) return;

        // 碰撞器中心绘制
        Gizmos.color = Color.yellow;

        Vector3 colliderCenter = transform.position + (Vector3)danmakuConfig.ColliderOffset;

        Gizmos.DrawSphere(transform.position, 0.01f);
        Gizmos.DrawLine(transform.position, colliderCenter);
        Gizmos.DrawSphere(colliderCenter, 0.01f);

        // 碰撞器绘制
        Gizmos.color = Color.green;
        switch (colliderType)
        {
            case E_ColliderType.None:
                break;
            case E_ColliderType.Rect:
                Gizmos.DrawWireCube(colliderCenter, size);
                break;
            case E_ColliderType.Circle:
                Gizmos.DrawWireSphere(colliderCenter, radius);
                break;
        }
    }
}
