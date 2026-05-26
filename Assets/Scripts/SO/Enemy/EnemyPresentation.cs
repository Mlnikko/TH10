using UnityEngine;

/// <summary>
/// 将 <see cref="EnemyConfig"/> 的 Sprite / Animator 应用到池化敌人 <see cref="EnemyPrefabArchetypes.Unit"/>。
/// </summary>
public static class EnemyPresentation
{
    public static void Apply(EnemyConfig config, GameObject root)
    {
        if (config == null || root == null)
            return;

        var spriteRenderer = PresentationActorResolve.ResolveSpriteRenderer(root);
        if (spriteRenderer != null)
        {
            if (config.displaySprite != null)
                spriteRenderer.sprite = config.displaySprite;

            PresentationHorizontalFlip.ResetToDefaultFacing(root);
        }

        if (config.animatorController == null)
            return;

        var animator = PresentationActorResolve.ResolveAnimator(root);
        if (animator == null)
            return;

        if (animator.runtimeAnimatorController == config.animatorController)
            return;

        animator.runtimeAnimatorController = config.animatorController;
        animator.Rebind();
        animator.Update(0f);
    }
}
