using UnityEngine;

public abstract class Hittable : MonoBehaviour , IHittable
{
    public E_ColliderType colliderType;

    public Rect GetColliderRect()
    {
        throw new System.NotImplementedException();
    }

    public string GetHittableType()
    {
        throw new System.NotImplementedException();
    }

    public void OnHit(Danmaku danmaku)
    {
        throw new System.NotImplementedException();
    }
}
