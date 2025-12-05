using UnityEngine;

public class PlayerPresentationUpdater : IPresentationUpdater
{
    readonly Transform _transform;
    public PlayerPresentationUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;
    }
    public void UpdatePresentation(in EntityManager em, Entity entity)
    {
        if (_transform == null) return;
        if (!em.IsValid(entity)) return;

        ref readonly var pos = ref em.GetComponentSpan<CPosition>()[entity.Index];
        _transform.position = new Vector3(pos.x, pos.y, 0);
    }
}