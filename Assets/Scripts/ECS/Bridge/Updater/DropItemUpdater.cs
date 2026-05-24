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

        if (!PresentationUpdaterHelper.TryGetDisplayTransform(em, entity, out float x, out float y, out float angleRad))
            return;

        _transform.SetPositionAndRotation(
            new Vector3(x, y, 0),
            Quaternion.Euler(0, 0, angleRad * Mathf.Rad2Deg));
    }
}
