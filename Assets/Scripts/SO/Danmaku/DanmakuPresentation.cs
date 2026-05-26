using UnityEngine;

/// <summary>
/// 将 <see cref="DanmakuConfig"/> 的表现字段应用到池化弹幕 GameObject（运行时与编辑器预览共用）。
/// </summary>
public static class DanmakuPresentation
{
    public static void Apply(DanmakuConfig config, GameObject root)
    {
        if (config == null)
            return;

        Apply(config.sprite, config.scale, root);
    }

    public static void Apply(Sprite sprite, float scale, GameObject root)
    {
        if (root == null)
            return;

        float uniformScale = Mathf.Max(scale, 0.01f);
        root.transform.localScale = Vector3.one * uniformScale;

        if (!root.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            spriteRenderer = root.GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer == null)
            return;

        if (sprite != null)
            spriteRenderer.sprite = sprite;

        spriteRenderer.color = Color.white;
    }
}
