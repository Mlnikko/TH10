using UnityEngine;

public interface ICollider
{
    E_ColliderLayer ColliderLayer { get;}
    E_ColliderType ColliderType { get;}
    Rect GetBounds();
    void UpdateColliderCenterPos(Vector3 pos);
    void OnCollisionEnter(ICollider other);
}
