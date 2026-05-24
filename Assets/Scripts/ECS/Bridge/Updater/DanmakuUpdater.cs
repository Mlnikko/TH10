using UnityEngine;

public class DanmakuUpdater : IGameObjectUpdater
{
    Transform _transform;

    public DanmakuUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        if (_transform != null
            && PresentationUpdaterHelper.TryGetDisplayTransform(em, entity, out float x, out float y, out float angleRad))
        {
            _transform.SetPositionAndRotation(
                new Vector3(x, y, 0),
                Quaternion.Euler(0, 0, angleRad * Mathf.Rad2Deg));
        }
    }
}
