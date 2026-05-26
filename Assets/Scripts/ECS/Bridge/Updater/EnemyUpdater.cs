using UnityEngine;

public class EnemyUpdater : IGameObjectUpdater
{
    readonly Transform _transform;
    readonly PresentationVelocityAnimatorSync.Driver _animatorDriver;
    readonly PresentationHorizontalFlip _horizontalFlip;

    public EnemyUpdater(GameObject gameObject)
    {
        _transform = gameObject.transform;

        var spriteRenderer = PresentationActorResolve.ResolveSpriteRenderer(gameObject);
        _horizontalFlip = spriteRenderer != null
            ? new PresentationHorizontalFlip(spriteRenderer)
            : null;

        var animator = PresentationActorResolve.ResolveAnimator(gameObject);
        _animatorDriver = animator != null
            ? new PresentationVelocityAnimatorSync.Driver(animator)
            : null;
    }

    public void UpdateGameObject(in EntityManager em, Entity entity)
    {
        if (_transform != null
            && PresentationUpdaterHelper.TryGetDisplayTransform(em, entity, out float x, out float y, out _))
        {
            _transform.position = new Vector3(x, y, 0);
        }

        if (!em.HasComponent<CVelocity>(entity))
            return;

        ref readonly var velocity = ref em.GetComponentSpan<CVelocity>()[entity.Index];
        _horizontalFlip?.Tick(velocity.vx);
        _animatorDriver?.Tick(velocity.vx, velocity.vy);
    }
}
