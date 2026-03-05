using UnityEngine;

[RequireComponent (typeof(SpriteRenderer))]
public class DanmakuConfigViewer : MonoBehaviour
{
    public DanmakuConfig danmakuConfig;

    [SerializeField] DanmakuType danmakuType;

    [Header("弹幕池大小")]
    [SerializeField] int poolSize;

    [Header("弹幕Transform设置")]
    [SerializeField] Vector2 localScale;
    [SerializeField] Vector3 localRotation;

    [Header("弹幕渲染设置")]
    [SerializeField] Sprite sprite;
    [SerializeField] Color color;

    [Header("弹幕碰撞器设置")]
    [SerializeField] ColliderConfig colliderConfig;

    [SerializeField] float damage;

    public void LoadDanmakuConfig()
    {
        if (danmakuConfig == null)
        {
            Logger.Warn("弹幕配置文件未设置", LogTag.Config);
            return;
        }

        poolSize = danmakuConfig.poolSize;

        localScale = danmakuConfig.localScale;
        localRotation = danmakuConfig.localRotation;

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
        danmakuConfig.poolSize = poolSize;
        danmakuConfig.localScale = localScale;
        danmakuConfig.localRotation = localRotation;

        danmakuConfig.sprite = sprite;
        danmakuConfig.color = color;
       
        danmakuConfig.danmakuType = danmakuType;

        danmakuConfig.colliderConfig = colliderConfig;

        danmakuConfig.damage = damage;

        Logger.Debug($"弹幕配置文件保存完成: {danmakuConfig.name}");
    }

    public void PreviewDanmaku()
    {
        LoadDanmakuConfig();

        // 预览缩放
        transform.localScale = localScale;
        transform.localRotation = Quaternion.Euler(localRotation);

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

        var colliderCenter = transform.position + (Vector3)colliderConfig.offset;

        Gizmos.DrawSphere(transform.position, 0.01f);
        Gizmos.DrawLine(transform.position, colliderCenter);
        Gizmos.DrawSphere(colliderCenter, 0.01f);

        // 碰撞器绘制
        Gizmos.color = Color.green;
        switch (colliderConfig.type)
        {
            case E_ColliderType.None:
                break;
            case E_ColliderType.Rect:
                Gizmos.DrawWireCube(colliderCenter, colliderConfig.boxSize);
                break;
            case E_ColliderType.Circle:
                Gizmos.DrawWireSphere(colliderCenter, colliderConfig.radius);
                break;
        }
    }
}
