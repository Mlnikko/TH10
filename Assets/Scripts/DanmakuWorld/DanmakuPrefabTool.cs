using UnityEngine;

[RequireComponent (typeof(SpriteRenderer))]
public class DanmakuPrefabTool : MonoBehaviour
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
    [SerializeField] E_ColliderType colliderType;
    [SerializeField] E_ColliderLayer colliderLayer;
    [SerializeField] Vector2 size;
    [SerializeField] float radius;
    [SerializeField] Vector2 colliderOffset;

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
       
        colliderType = danmakuConfig.colliderType;
        colliderLayer = danmakuConfig.colliderLayer;
        size = danmakuConfig.size;
        radius = danmakuConfig.radius;
        colliderOffset = danmakuConfig.colliderOffset;
        danmakuType = danmakuConfig.danmakuType;
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
       
        danmakuConfig.colliderType = colliderType;
        danmakuConfig.colliderLayer = colliderLayer;

        danmakuConfig.size = size;
        danmakuConfig.radius = radius;
        danmakuConfig.colliderOffset = colliderOffset;
        danmakuConfig.danmakuType = danmakuType;
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

        Vector3 colliderCenter = transform.position + (Vector3)colliderOffset;

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
