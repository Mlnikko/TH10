using UnityEngine;

public class DanmakuUpdater : IGameObjectUpdater
{
    Transform transform;
    SpriteRenderer spriteRenderer;

    public DanmakuUpdater(GameObject gameObject)
    {
        transform = gameObject.transform;
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        
    }
}
