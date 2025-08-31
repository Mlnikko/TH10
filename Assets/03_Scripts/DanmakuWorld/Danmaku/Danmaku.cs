using UnityEngine;

public enum E_DanmakuCamp
{
    None,
    Player,
    Enemy
}

public enum E_DanmakuType
{
    Normal,
    Homing
}

[RequireComponent (typeof(SpriteRenderer))]
public class Danmaku : MonoBehaviour
{
    public DanmakuConfig danmakuConfig;
    protected SpriteRenderer spriteRenderer;
    protected ObjectPool<Danmaku> danmakuPool;

    public Vector3 Position
    {
        get { return transform.position; }
        set 
        { 
            transform.position = value; 
            UpdateColliderCenterPos();
        }
    }
    public Vector3 LocalPosition
    {
        get { return transform.localPosition; }
        set { transform.localPosition = value; }
    }
    public Vector3 Rotation
    {
        get { return transform.rotation.eulerAngles; }
        set { transform.rotation = Quaternion.Euler(value); }
    }
    public Vector3 LocalRotation
    {
        get { return transform.localRotation.eulerAngles; }
        set { transform.localRotation = Quaternion.Euler(value); }
    }

    public Vector3 Velocity;

    public Vector3 ColliderCenter
    {
        get
        {
            return transform.position + (Vector3)danmakuConfig.ColliderOffset;
        }
    }

    public Vector2 RectColliderSize
    {
        get
        {
            return danmakuConfig.Size;
        }
    }


    public ICollider Collider { get; private set; }
    public E_DanmakuType DanmakuType
    {
        get { return danmakuConfig.DanmakuType; }
    }
   

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();      
    }

    public void InitDanmaku(ObjectPool<Danmaku> pool, E_DanmakuCamp camp)
    {
        danmakuPool = pool;
        spriteRenderer.sprite = danmakuConfig.Sprite;
        spriteRenderer.color = danmakuConfig.Color;
        transform.localScale = danmakuConfig.LocalScale;

        InitDanmakuCollider(danmakuConfig.ColliderType, camp);

        SetActive(false);
    }

    void InitDanmakuCollider(E_ColliderType colliderType, E_DanmakuCamp camp)
    {
        E_ColliderLayer layer = E_ColliderLayer.Default;

        switch (camp)
        {
            case E_DanmakuCamp.None:
                layer = E_ColliderLayer.Default;
                break;
            case E_DanmakuCamp.Player:
                layer = E_ColliderLayer.PlayerDanmaku;
                break;
            case E_DanmakuCamp.Enemy:
                layer = E_ColliderLayer.EnemyDanmaku;
                break;
        }

        switch (colliderType)
        {
            case E_ColliderType.None:
                break;
            case E_ColliderType.Rect:
                Collider = new RectCollider(layer, ColliderCenter, danmakuConfig.Size);
                break;
            case E_ColliderType.Circle:
                Collider = new CircleCollider(layer, ColliderCenter, danmakuConfig.Radius);
                break;
        }
    }

    public void UpdateColliderCenterPos()
    {
        Collider?.UpdateColliderCenterPos(ColliderCenter);
    }

    public void SetActive(bool enable)
    {
        gameObject.SetActive(enable);
    }

    public void Release()
    {
        if (danmakuPool == null) return;
        danmakuPool.Release(this);
    } 
}
