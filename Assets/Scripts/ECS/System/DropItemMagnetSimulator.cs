using System;

/// <summary>道具吸收飞行（确定性，供 <see cref="DropItemMagnetSystem"/> 使用）。</summary>
public static class DropItemMagnetSimulator
{
    public static bool StepTowardTarget(
        ref CPosition dropPos,
        float targetX,
        float targetY,
        float speedPerFrame,
        float pickupRadius,
        out bool reached)
    {
        reached = false;
        if (speedPerFrame <= 0f)
            return false;

        float dx = targetX - dropPos.x;
        float dy = targetY - dropPos.y;
        float distSq = dx * dx + dy * dy;
        float pickupRadiusSq = pickupRadius * pickupRadius;

        if (distSq <= pickupRadiusSq)
        {
            dropPos.x = targetX;
            dropPos.y = targetY;
            reached = true;
            return true;
        }

        float dist = MathF.Sqrt(distSq);
        if (dist <= 0f)
        {
            reached = true;
            return true;
        }

        if (speedPerFrame >= dist)
        {
            dropPos.x = targetX;
            dropPos.y = targetY;
            reached = true;
            return true;
        }

        float invDist = 1f / dist;
        dropPos.x += dx * invDist * speedPerFrame;
        dropPos.y += dy * invDist * speedPerFrame;
        return true;
    }
}
