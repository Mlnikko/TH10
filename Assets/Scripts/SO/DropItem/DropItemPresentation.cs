using UnityEngine;

/// <summary>
/// 将 <see cref="DropItemConfig"/> 的表现字段应用到池化掉落物 GameObject（运行时与编辑器预览共用）。
/// </summary>
public static class DropItemPresentation
{
    public static void Apply(DropItemConfig config, GameObject root)
    {
        if (config == null || root == null)
            return;

        if (!root.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            spriteRenderer = root.GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer == null)
            return;

        if (config.pickupSprite != null)
            spriteRenderer.sprite = config.pickupSprite;

        spriteRenderer.color = Color.white;
    }
}
