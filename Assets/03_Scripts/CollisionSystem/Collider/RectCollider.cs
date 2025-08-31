using UnityEngine;
public class RectCollider : Collider
{
    public Vector2 Size;

    public RectCollider(E_ColliderLayer layer, Vector2 center, Vector2 size): base(layer, E_ColliderType.Rect, center)
    {
        Size = size;
    }
    public override Rect GetBounds()
    {
        return new Rect(CenterPos.x - Size.x / 2, CenterPos.y - Size.y / 2, Size.x, Size.y);
    }
}
