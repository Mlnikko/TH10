using UnityEngine;

public class BattleRect
{
    public static Vector2 Center { get; private set; }
    public static Vector2 Size { get; private set; }

    public static Vector2 TopLeft { get; private set; }
    public static Vector2 TopRight { get; private set; }
    public static Vector2 BottomLeft { get; private set; }
    public static Vector2 BottomRight { get; private set; }

    public static float Top {  get; private set; }
    public static float Bottom {  get; private set; }
    public static float Left {  get; private set; }
    public static float Right {  get; private set; }

    public BattleRect(Vector2 center, Vector2 size)
    {
        Center = center;
        Size = size;

        TopLeft = new(center.x - size.x / 2, center.y + size.y / 2);
        TopRight = center + size / 2;
        BottomLeft = center - size / 2;
        BottomRight = new(center.x + size.x / 2, center.y - size.y / 2);

        Top = center.y + size.y / 2;
        Bottom = center.y - size.y / 2;
        Left = center.x - size.x / 2;
        Right = center.x + size.x / 2;
    }

    public bool IsOutOfRect(Vector2 pos, Vector2 thickness)
    {
        if (pos.x > Right + thickness.x) return true;
        if (pos.x < Left - thickness.x) return true;
        if (pos.y > Top + thickness.y) return true;
        if (pos.y < Bottom - thickness.y) return true;
        return false;
    }
}

public class DanmakuArea : MonoBehaviour
{
    public BattleRect battleRect;
    public Vector2 boundsThickness;
    public bool IsOutOfBounds(Vector2 pos)
    {
        if (battleRect == null) return false;
        return battleRect.IsOutOfRect(pos, boundsThickness);
    }
    void Awake()
    {
        battleRect = new(transform.position, transform.localScale);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 danmakuBounds = transform.localScale + (Vector3)boundsThickness * 2;
        Gizmos.DrawWireCube(transform.position, danmakuBounds);
    }
}
