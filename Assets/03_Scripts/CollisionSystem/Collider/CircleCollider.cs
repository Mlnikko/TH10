using UnityEngine;

public class CircleCollider : Collider
{
    public float Radius;

    public CircleCollider(E_ColliderLayer layer, Vector2 center, float radius) : base(layer, E_ColliderType.Circle, center)
    {
        Radius = radius;
    }

    public override Rect GetBounds()
    {
        return new Rect(CenterPos.x - Radius, CenterPos.y - Radius, Radius * 2, Radius * 2);
    }
}
