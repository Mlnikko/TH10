using UnityEngine;

/// <summary>战斗背景云雾池化实例的表现应用（不参与 ECS）。</summary>
public static class BattleStageCloudPresentation
{
    public static void Apply(GameObject root, Sprite sprite, BattleAreaCloudLayerData cfg, float uniformScale)
    {
        if (root == null || sprite == null || cfg == null)
            return;

        if (!root.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            spriteRenderer = root.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = cfg.sortingOrder;
        var color = spriteRenderer.color;
        color.a = cfg.alpha;
        spriteRenderer.color = color;
        root.transform.localScale = Vector3.one * uniformScale;
    }
}
