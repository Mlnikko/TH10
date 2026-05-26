using UnityEngine;

/// <summary>从池化实体根节点解析 SpriteRenderer / Animator（敌人与表现 Apply 共用）。</summary>
public static class PresentationActorResolve
{
    public static SpriteRenderer ResolveSpriteRenderer(GameObject root)
    {
        if (root == null)
            return null;

        if (root.TryGetComponent<SpriteRenderer>(out var onRoot))
            return onRoot;

        return root.GetComponentInChildren<SpriteRenderer>(true);
    }

    public static Animator ResolveAnimator(GameObject root)
    {
        if (root == null)
            return null;

        if (root.TryGetComponent<Animator>(out var onRoot))
            return onRoot;

        return root.GetComponentInChildren<Animator>(true);
    }
}
