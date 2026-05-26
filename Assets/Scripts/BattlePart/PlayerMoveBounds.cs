using UnityEngine;

/// <summary>
/// 按 <see cref="CharacterConfig.moveColliderConfig"/> 将玩家锚点限制在战斗区内，
/// 使移动碰撞盒整体不越出可玩矩形。
/// </summary>
public static class PlayerMoveBounds
{
    public static void BakeFromConfig(in ColliderConfig config, out byte shape, out float offsetX, out float offsetY,
        out float radius, out float halfW, out float halfH)
    {
        shape = (byte)config.shape;
        offsetX = config.offset.x;
        offsetY = config.offset.y;
        radius = Mathf.Max(0f, config.radius);
        halfW = Mathf.Max(0f, config.boxSize.x * 0.5f);
        halfH = Mathf.Max(0f, config.boxSize.y * 0.5f);
    }

    public static void ClampAnchorToBattleArea(
        ref float posX,
        ref float posY,
        in BattleAreaData area,
        byte moveShape,
        float moveOffsetX,
        float moveOffsetY,
        float moveRadius,
        float moveHalfW,
        float moveHalfH)
    {
        if (!GlobalBattleData.IsInitialized)
            return;

        ResolveAnchorLimits(
            area,
            moveShape,
            moveOffsetX,
            moveOffsetY,
            moveRadius,
            moveHalfW,
            moveHalfH,
            out float minX,
            out float maxX,
            out float minY,
            out float maxY);

        posX = Mathf.Clamp(posX, minX, maxX);
        posY = Mathf.Clamp(posY, minY, maxY);
    }

    public static void ResolveAnchorLimits(
        in BattleAreaData area,
        byte moveShape,
        float moveOffsetX,
        float moveOffsetY,
        float moveRadius,
        float moveHalfW,
        float moveHalfH,
        out float minAnchorX,
        out float maxAnchorX,
        out float minAnchorY,
        out float maxAnchorY)
    {
        var shape = (E_ColliderShape)moveShape;
        if (shape == E_ColliderShape.Circle && moveRadius > 0f)
        {
            minAnchorX = area.Left - moveOffsetX + moveRadius;
            maxAnchorX = area.Right - moveOffsetX - moveRadius;
            minAnchorY = area.Bottom - moveOffsetY + moveRadius;
            maxAnchorY = area.Top - moveOffsetY - moveRadius;
        }
        else if (shape == E_ColliderShape.Rect && (moveHalfW > 0f || moveHalfH > 0f))
        {
            minAnchorX = area.Left - moveOffsetX + moveHalfW;
            maxAnchorX = area.Right - moveOffsetX - moveHalfW;
            minAnchorY = area.Bottom - moveOffsetY + moveHalfH;
            maxAnchorY = area.Top - moveOffsetY - moveHalfH;
        }
        else
        {
            minAnchorX = area.Left;
            maxAnchorX = area.Right;
            minAnchorY = area.Bottom;
            maxAnchorY = area.Top;
        }

        if (minAnchorX > maxAnchorX)
        {
            float mid = (minAnchorX + maxAnchorX) * 0.5f;
            minAnchorX = maxAnchorX = mid;
        }

        if (minAnchorY > maxAnchorY)
        {
            float mid = (minAnchorY + maxAnchorY) * 0.5f;
            minAnchorY = maxAnchorY = mid;
        }
    }
}
