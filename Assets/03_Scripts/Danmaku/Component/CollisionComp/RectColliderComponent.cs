using UnityEngine;

public class RectColliderComponent : ColliderComponent
{
    public Vector3 Size;
    public RectColliderComponent(E_ColliderType colliderType, Vector2 offset, Vector3 size) : base(colliderType, offset)
    {
        ColliderType = colliderType;
        Size = size;
    }
}
