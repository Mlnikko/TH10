using UnityEngine;
public class ColliderComponent
{
    public E_ColliderType ColliderType;
    public Vector2 Offset;

    public ColliderComponent(E_ColliderType colliderType, Vector2 offset)
    {
        ColliderType = colliderType;
        Offset = offset;
    }
}
