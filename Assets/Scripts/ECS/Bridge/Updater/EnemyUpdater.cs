using UnityEngine;

public class EnemyUpdater : IGameObjectUpdater
{
    Transform _transform;

    public EnemyUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        if (_transform != null
            && PresentationUpdaterHelper.TryGetDisplayTransform(em, entity, out float x, out float y, out _))
        {
            _transform.position = new Vector3(x, y, 0);
        }
    }
}
