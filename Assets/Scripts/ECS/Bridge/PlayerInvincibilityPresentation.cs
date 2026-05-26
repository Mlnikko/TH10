using UnityEngine;

/// <summary>无敌期间角色精灵透明度闪动（表现层）。</summary>
public static class PlayerInvincibilityPresentation
{
    const float DimAlpha = 0.35f;
    const float FullAlpha = 1f;
    const int BlinkHalfPeriodFrames = 4;

    public static void ApplyBlink(SpriteRenderer[] renderers, uint logicFrame, int invincibleFramesRemaining)
    {
        if (renderers == null || renderers.Length == 0)
            return;

        float alpha = FullAlpha;
        if (invincibleFramesRemaining > 0)
        {
            bool dim = (logicFrame / BlinkHalfPeriodFrames) % 2 != 0;
            alpha = dim ? DimAlpha : FullAlpha;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
                continue;

            Color c = renderer.color;
            c.a = alpha;
            renderer.color = c;
        }
    }
}
