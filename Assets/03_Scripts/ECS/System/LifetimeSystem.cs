/// <summary>
/// 生命周期系统：处理实体的生存时间，自动销毁过期实体
/// </summary>
public class LifetimeSystem : BaseSystem
{
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        if (!Enabled) return;

        var lifetimes = EntityManager.GetComponentSpan<CLifetime>();
        var isActive = EntityManager.ActiveEntities;

        // 遍历所有活跃实体，减少生存时间
        for (int i = 0; i < EntityManager.MAX_ENTITIES; i++)
        {
            if (!isActive[i]) continue;

            // 检查是否有生命周期组件（ttl > 0 表示有效）
            if (lifetimes[i].ttl > 0)
            {
                lifetimes[i].ttl -= fixedDeltaTime;

                // 生存时间耗尽，销毁实体
                if (lifetimes[i].ttl <= 0)
                {
                    Entity entity = EntityManager.GetEntityByIndex(i);
                    if (!entity.IsNull)
                    {
                        EntityManager.DestroyEntity(entity);
                    }
                }
            }
        }
    }
}

