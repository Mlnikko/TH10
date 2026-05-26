using UnityEngine;

/// <summary>
/// 将 <see cref="EnemyConfig"/> 的表现字段应用到池化敌人 GameObject（运行时与编辑器预览共用）。
/// </summary>
public static class EnemyPresentation
{
    public static void Apply(EnemyConfig config, GameObject root)
    {
        if (config == null)
            return;

        Apply(config.displaySprite, config.displayColor, config.displayScale, config.animatorController, root);
    }

    public static void Apply(
        Sprite sprite,
        Color color,
        float scale,
        RuntimeAnimatorController animatorController,
        GameObject root)
    {
        if (root == null)
            return;

        float uniformScale = Mathf.Max(scale, 0.01f);
        root.transform.localScale = Vector3.one * uniformScale;

        if (!root.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            spriteRenderer = root.GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
        {
            if (sprite != null)
                spriteRenderer.sprite = sprite;

            spriteRenderer.color = color;
        }

        if (animatorController == null)
            return;

        if (!root.TryGetComponent<Animator>(out var animator))
            animator = root.GetComponentInChildren<Animator>(true);

        if (animator == null)
            return;

        if (animator.runtimeAnimatorController != animatorController)
        {
            animator.runtimeAnimatorController = animatorController;
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
