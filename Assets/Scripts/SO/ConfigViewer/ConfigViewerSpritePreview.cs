using UnityEngine;

/// <summary>
/// 配置 Viewer 共用的 Sprite 场景预览。
/// </summary>
public static class ConfigViewerSpritePreview
{
    public static void Apply(
        Transform target,
        Sprite sprite,
        Color color,
        float uniformScale = 1f)
    {
        if (uniformScale > 0f)
            target.localScale = Vector3.one * uniformScale;

        if (!target.TryGetComponent<SpriteRenderer>(out var renderer))
            return;

        if (sprite != null)
            renderer.sprite = sprite;

        renderer.color = color;
    }

    public static void ApplySortingFrom(Transform target, Transform reference)
    {
        if (!target.TryGetComponent<SpriteRenderer>(out var targetRenderer))
            return;

        if (reference != null && reference.TryGetComponent<SpriteRenderer>(out var refRenderer))
        {
            targetRenderer.sortingLayerID = refRenderer.sortingLayerID;
            targetRenderer.sortingOrder = refRenderer.sortingOrder + 1;
        }
        else
        {
            targetRenderer.sortingOrder = 10;
        }
    }
}
