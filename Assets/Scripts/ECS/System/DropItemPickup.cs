/// <summary>
/// 掉落物拾取与回收（碰撞拾取、道具吸收线共用）。
/// </summary>
public static class DropItemPickup
{
    public static bool TryCollisionPickup(
        EntityManager em,
        Entity dropEntity,
        Entity playerEntity,
        System.Span<CCollider> colliders)
    {
        if (!em.IsValid(dropEntity) || !em.IsValid(playerEntity))
            return false;
        if (!em.HasComponent<CDropItem>(dropEntity) || !em.HasComponent<CPlayer>(playerEntity))
            return false;

        int di = dropEntity.Index;
        ref readonly var dropCol = ref colliders[di];
        if (dropCol.layer != E_ColliderLayer.Item)
            return false;

        if (TempBitSets.DropItemPickupConsumed.Get(di))
            return false;

        ApplyPickupEffects(em, dropEntity, playerEntity);
        return TryConsumeDrop(em, dropEntity);
    }

  /// <summary>对单个掉落物应用拾取效果（不回收）。</summary>
    public static void ApplyPickupEffects(EntityManager em, Entity dropEntity, Entity playerEntity)
    {
        if (!em.IsValid(dropEntity) || !em.IsValid(playerEntity))
            return;
        if (!em.HasComponent<CDropItem>(dropEntity))
            return;

        ref readonly var dropComp = ref em.GetComponentSpan<CDropItem>()[dropEntity.Index];
        var cfg = GameResDB.Instance.GetConfig<DropItemConfig>(dropComp.cfgIndex);
        if (cfg == null)
            return;

        DropItemPickupEffects.Apply(in cfg, em, playerEntity);
    }

    public static bool TryConsumeDrop(EntityManager em, Entity dropEntity)
    {
        if (!em.IsValid(dropEntity))
            return false;

        int di = dropEntity.Index;
        if (TempBitSets.DropItemPickupConsumed.Get(di))
            return false;

        TempBitSets.DropItemPickupConsumed.Set(di, true);
        em.AddComponent(di, new CPoolRecycleTag());
        return true;
    }
}
