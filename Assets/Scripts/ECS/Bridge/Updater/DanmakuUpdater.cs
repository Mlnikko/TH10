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
        // === Î»ÖÃ¸üÐÂ ===
        if (_transform != null)
        {
            var pos = em.GetComponentSpan<CPosition>()[entity.Index];
            var rot = em.GetComponentSpan<CRotation>()[entity.Index];
            _transform.SetPositionAndRotation(new Vector3(pos.x, pos.y, 0), Quaternion.Euler(0, 0, rot.rotZ));
        }
    }
}
