public class MovementSystem : BaseSystem
{
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        if (!Enabled) return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();
        var isActive = EntityManager.ActiveEntities;

        for (int i = 0; i < EntityManager.MAX_ENTITIES; i++)
        {
            if (!isActive[i]) continue;

            positions[i].x += velocities[i].vx * fixedDeltaTime;
            positions[i].y += velocities[i].vy * fixedDeltaTime;
        }
    }
}