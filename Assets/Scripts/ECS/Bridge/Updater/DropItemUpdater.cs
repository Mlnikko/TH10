using UnityEngine;

public class DropItemUpdater : IGameObjectUpdater
{
    readonly Transform _transform;

    public DropItemUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        if (_transform == null)
            return;

        var pos = em.GetComponentSpan<CPosition>()[entity.Index];
        var rot = em.GetComponentSpan<CRotation>()[entity.Index];
        _transform.SetPositionAndRotation(
            new Vector3(pos.x, pos.y, 0),
            Quaternion.Euler(0, 0, rot.angleRad * Mathf.Rad2Deg));
    }
}
