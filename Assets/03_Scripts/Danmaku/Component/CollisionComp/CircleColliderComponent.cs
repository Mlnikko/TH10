using UnityEngine;

public class CircleColliderComponent : ColliderComponent
{
    public float Radius;

    public CircleColliderComponent(E_ColliderType colliderType, Vector2 offset, float radius) : base(colliderType, offset)
    {
        ColliderType = E_ColliderType.Circle;
        Radius = radius;
    }
}
