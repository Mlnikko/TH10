using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DanmakuPrefabTool : MonoBehaviour
{
    [SerializeField] DanmakuConfig danmakuConfig;
    [Header("µ¯Ä»Ô¤ÖÆÌåËõ·ÅÉèÖÃ")]
    [SerializeField] Vector3 localScale;

    [Header("µ¯Ä»äÖÈ¾ÉèÖÃ")]
    [SerializeField] Sprite sprite;
    [SerializeField] Color color;

    [Header("µ¯Ä»Åö×²Æ÷ÉèÖÃ")]
    [SerializeField] Vector2 colliderOffset;
    [SerializeField] E_ColliderType colliderType;
    [SerializeField] Vector2 size;
    [SerializeField] float radius;

    [Header("µ¯Ä»ÀàĞÍ")]
    [SerializeField] E_DanmakuType danmakuType;

    Vector2 colliderCenter
    {
        get { return (Vector2)transform.position + colliderOffset; }
    }

    public void LoadDanmakuConfig()
    {
        if (danmakuConfig == null)
        {
            Debug.LogWarning("µ¯Ä»ÅäÖÃÎÄ¼şÎ´ÉèÖÃ");
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
    }

    public void SaveDanmakuConfig()
    {
        if (danmakuConfig == null)
        {
            Debug.LogWarning("µ¯Ä»ÅäÖÃÎÄ¼şÎ´ÉèÖÃ");
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
    }

    public void PreviewDanmaku()
    {
        LoadDanmakuConfig();
        // Ô¤ÀÀËõ·Å
        transform.localScale = localScale;
        // Ô¤ÀÀäÖÈ¾
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Åö×²Æ÷ÖĞĞÄ»æÖÆ
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.01f);
        Gizmos.DrawLine(transform.position, colliderCenter);
        Gizmos.DrawSphere(colliderCenter, 0.01f);

        // Åö×²Æ÷»æÖÆ
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
