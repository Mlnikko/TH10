using UnityEngine;

/// <summary>
/// 单向右移动画：按水平速度设置 <see cref="SpriteRenderer.flipX"/>，向左时镜像精灵。
/// </summary>
public sealed class PresentationHorizontalFlip
{
    const float DefaultVelocityThreshold = 0.001f;

    readonly SpriteRenderer _spriteRenderer;
    readonly bool _invertFacing;
    bool _flipX;

    public PresentationHorizontalFlip(SpriteRenderer spriteRenderer, bool invertFacing = false)
    {
        _invertFacing = invertFacing;
        _spriteRenderer = spriteRenderer;
        _flipX = spriteRenderer != null && spriteRenderer.flipX;
    }

    public static void ResetToDefaultFacing(GameObject root, bool invertFacing = false)
    {
        var spriteRenderer = PresentationActorResolve.ResolveSpriteRenderer(root);
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = invertFacing;
    }

    public void Tick(float vx, float velocityThreshold = DefaultVelocityThreshold)
    {
        if (_spriteRenderer == null)
            return;

        bool flipX = _flipX;
        if (vx > velocityThreshold)
            flipX = _invertFacing;
        else if (vx < -velocityThreshold)
            flipX = !_invertFacing;

        if (flipX == _flipX)
            return;

        _flipX = flipX;
        _spriteRenderer.flipX = flipX;
    }
}
