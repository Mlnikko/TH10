using UnityEngine;

public class DanmakuUpdater : IGameObjectUpdater
{
    Transform _transform;
    SpriteRenderer _spriteRenderer;

    public DanmakuUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        // === Î»ÖÃ¸üÐÂ ===
        if (_transform != null)
        {
            var pos = em.GetComponentSpan<CPosition>()[entity.Index];
            _transform.position = new Vector3(pos.x, pos.y, 0);
        }
    }
}
