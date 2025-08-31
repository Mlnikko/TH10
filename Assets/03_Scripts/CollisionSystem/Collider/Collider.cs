using System;
using UnityEngine;

public abstract class Collider : ICollider
{
    public Vector3 CenterPos;
    public event Action OnCollide;
    public E_ColliderLayer ColliderLayer { get; }
    public E_ColliderType ColliderType { get; }

    public Collider(E_ColliderLayer layer, E_ColliderType type, Vector3 centerPos)
    {
        ColliderLayer = layer;
        ColliderType = type;
        CenterPos = centerPos;
    }

    public abstract Rect GetBounds();

    public void OnCollisionEnter(ICollider other)
    {
        OnCollide?.Invoke();
    }

    public void UpdateColliderCenterPos(Vector3 pos)
    {
        CenterPos = pos;
    }
}
