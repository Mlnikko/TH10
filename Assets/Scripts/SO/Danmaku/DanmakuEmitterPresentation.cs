using UnityEngine;

/// <summary>
/// 将 <see cref="DanmakuEmitterConfig"/> 的 displaySprite 应用到池化发射器 GameObject。
/// 循环缩放由 <see cref="DanmakuEmitterDisplaySpin"/> / 武器布局逻辑处理，此处仅重置基准缩放。
/// </summary>
public static class DanmakuEmitterPresentation
{
    public static void Apply(DanmakuEmitterConfig config, GameObject root)
    {
        if (config == null)
            return;

        Apply(config.displaySprite, root);
    }

    public static void Apply(Sprite displaySprite, GameObject root)
    {
        if (root == null)
            return;

        root.transform.localScale = Vector3.one;

        if (!root.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            spriteRenderer = root.GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = displaySprite;
        spriteRenderer.color = Color.white;
    }
}
