using UnityEngine;

[RequireComponent (typeof(SpriteRenderer))]
public abstract class DanmakuPrefab : MonoBehaviour
{
    public DanmakuConfig DanmakuConfig
    {
        get
        {
            return danmakuConfig;
        }
    }

    [SerializeField] protected DanmakuConfig danmakuConfig;
    SpriteRenderer spriteRenderer;

    [Header("µØƒª‘§÷∆ÃÂÀı∑≈…Ë÷√")]
    [SerializeField] protected Vector3 localScale;

    [Header("µØƒª‰÷»æ…Ë÷√")]
    [SerializeField] protected Sprite sprite;
    [SerializeField] protected Color color;

    [Header("µØƒª≈ˆ◊≤∆˜…Ë÷√")]
    [SerializeField] protected Vector2 colliderOffset;
    [SerializeField] protected E_ColliderType colliderType;
    public Vector3 ColliderCenter
    {
        get
        {
            return transform.position + (Vector3)colliderOffset;
        }
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        LoadDanmakuConfig();
        InitPrefab();
    }
    protected virtual void InitPrefab()
    {
        spriteRenderer.sprite = danmakuConfig.Sprite;
        spriteRenderer.color = danmakuConfig.Color;
        transform.localScale = localScale;
    }

    public virtual void PreviewDanmaku()
    {
        if(spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        transform.localScale = localScale;  
    }
    public void SaveDanmakuConfig()
    {
        if (danmakuConfig == null) return;

        danmakuConfig.LocalScale = localScale;
        danmakuConfig.Sprite = sprite;
        danmakuConfig.Color = color;
        danmakuConfig.ColliderOffset = colliderOffset;
        danmakuConfig.ColliderType = colliderType;

        OnDanmakuConfigSave();

        Debug.Log("“—±£¥ÊµØƒª≈‰÷√" + danmakuConfig.name);
    }
    protected abstract void OnDanmakuConfigSave();

    public void LoadDanmakuConfig()
    {
        if (danmakuConfig == null) return;

        localScale = danmakuConfig.LocalScale;
        sprite = danmakuConfig.Sprite;
        color = danmakuConfig.Color;
        colliderOffset = danmakuConfig.ColliderOffset;  
        colliderType = danmakuConfig.ColliderType;

        OnDanmakuConfigLoad();

        Debug.Log("“—º”‘ÿµØƒª≈‰÷√" + danmakuConfig.name);
    }

    protected abstract void OnDanmakuConfigLoad();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, ColliderCenter);
        Gizmos.DrawSphere(ColliderCenter, 0.01f);
    }
}
