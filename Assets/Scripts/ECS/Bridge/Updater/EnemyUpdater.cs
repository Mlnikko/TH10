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
        // === 位置更新 ===
        if (_transform != null)
        {
            var pos = em.GetComponentSpan<CPosition>()[entity.Index];
            var rot = em.GetComponentSpan<CRotation>()[entity.Index];
            _transform.SetPositionAndRotation(new Vector3(pos.x, pos.y, 0), Quaternion.Euler(0, 0, rot.angle));
        }
    }
}
