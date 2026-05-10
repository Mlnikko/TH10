using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)] // 强制紧凑排列
public struct CollisionEvent
{
    // 1. 核心索引 (8 字节)
    public Entity EntityA, EntityB; // 碰撞双方实体ID

    // 2. 必要几何数据 (8 字节) - 用于计算特效位置/朝向/击退
    public float ContactX, ContactY; // 碰撞点坐标

#if UNITY_EDITOR
    public uint Frame; // 4 字节 - 仅编辑器用于调试
#endif
}

public static class CollisionEventBuffer
{
    const int MAX_EVENTS = 2048;
    static CollisionEvent[] _events = new CollisionEvent[MAX_EVENTS];
    static int _count = 0;

    public static int Count => _count;

    public static void Clear()
    {
        _count = 0;
    }

    public static bool Add(CollisionEvent evt)
    {
        if (_count >= MAX_EVENTS)
        {
            Logger.Error("[Collision] Event buffer overflow!", LogTag.Collision);
            return false;
        }
        _events[_count++] = evt;
        return true;
    }

    public static Span<CollisionEvent> GetEvents()
    {
        return new Span<CollisionEvent>(_events, 0, _count);
    }
}

public class CollisionLogicSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        // 须每帧清空：无碰撞事件时若提前 return，会导致索引复用后误判「已命中 / 已拾取」。
        TempBitSets.PlayerDanmakuHitConsumed.ClearAll();
        TempBitSets.DropItemPickupConsumed.ClearAll();

        var events = CollisionEventBuffer.GetEvents();
        if (events.Length == 0) return;

        var colliders = EntityManager.GetComponentSpan<CCollider>();

        for (int i = 0; i < events.Length; i++)
        {
            ref readonly var evt = ref events[i];
            TryApplyPlayerDanmakuVsEnemy(evt.EntityA, evt.EntityB, colliders);
            TryApplyPlayerDanmakuVsEnemy(evt.EntityB, evt.EntityA, colliders);
            TryApplyDropPickup(evt.EntityA, evt.EntityB, colliders);
            TryApplyDropPickup(evt.EntityB, evt.EntityA, colliders);
        }
    }

    /// <summary>
    /// 判定 bullet 是否为玩家弹幕且 victim 为敌人；扣血、销毁弹幕；敌人血量 ≤ 0 时标记回收（与弹幕越界回收同一套表现管线）。
    /// </summary>
    void TryApplyPlayerDanmakuVsEnemy(Entity bulletEntity, Entity enemyEntity, Span<CCollider> colliders)
    {
        if (!EntityManager.IsValid(bulletEntity) || !EntityManager.IsValid(enemyEntity))
            return;
        if (!EntityManager.HasComponent<CDanmaku>(bulletEntity) || !EntityManager.HasComponent<CEnemy>(enemyEntity))
            return;

        int bi = bulletEntity.Index;
        ref readonly var bulletCol = ref colliders[bi];
        if (bulletCol.layer != E_ColliderLayer.PlayerDanmaku)
            return;

        if (TempBitSets.PlayerDanmakuHitConsumed.Get(bi))
            return;

        ref readonly var danmaku = ref EntityManager.GetComponentSpan<CDanmaku>()[bi];
        var dmgCfg = GameResDB.Instance.GetConfig<DanmakuConfig>(danmaku.cfgIndex);
        if (dmgCfg == null)
            return;

        int damage = Math.Max(0, (int)MathF.Round(dmgCfg.damage));

        ref var enemy = ref EntityManager.GetComponent<CEnemy>(enemyEntity);
        enemy.currentHealth -= damage;

        TempBitSets.PlayerDanmakuHitConsumed.Set(bi, true);
        EntityManager.AddComponent(bi, new CPoolRecycleTag());

        if (enemy.currentHealth <= 0)
        {
            TrySpawnDropsFromEnemy(enemyEntity);
            EntityManager.AddComponent(enemyEntity.Index, new CPoolRecycleTag());
        }
    }

    void TrySpawnDropsFromEnemy(Entity enemyEntity)
    {
        ref readonly var ce = ref EntityManager.GetComponent<CEnemy>(enemyEntity);
        var enemyCfg = GameResDB.Instance.GetConfig<EnemyConfig>(ce.enemyCfgIndex);
        ref readonly var pos = ref EntityManager.GetComponent<CPosition>(enemyEntity);

        int[] enemyDrops = null;
        if (enemyCfg != null && enemyCfg.dropOnDeathCfgIndices != null && enemyCfg.dropOnDeathCfgIndices.Length > 0)
            enemyDrops = enemyCfg.dropOnDeathCfgIndices;

        E_WaveDropOverrideMode mode = E_WaveDropOverrideMode.UseEnemyConfig;
        int[] waveDrops = null;
        if (EntityManager.HasComponent<CEnemyDeathLoot>(enemyEntity))
        {
            ref readonly var loot = ref EntityManager.GetComponent<CEnemyDeathLoot>(enemyEntity);
            mode = loot.waveDropMode;
            waveDrops = loot.waveDropCfgIndices;
        }

        switch (mode)
        {
            case E_WaveDropOverrideMode.UseEnemyConfig:
                SpawnDropsFromIndexArray(pos.x, pos.y, enemyDrops);
                break;
            case E_WaveDropOverrideMode.Replace:
                SpawnDropsFromIndexArray(pos.x, pos.y, waveDrops);
                break;
            case E_WaveDropOverrideMode.Append:
                SpawnDropsFromTwoIndexArrays(pos.x, pos.y, enemyDrops, waveDrops);
                break;
        }
    }

    static int CountPositiveDropIndices(int[] arr)
    {
        if (arr == null)
            return 0;
        int c = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] >= 0)
                c++;
        }
        return c;
    }

    void SpawnDropsFromIndexArray(float x, float y, int[] indices)
    {
        if (indices == null || indices.Length == 0)
            return;
        int total = CountPositiveDropIndices(indices);
        if (total == 0)
            return;
        int k = 0;
        for (int i = 0; i < indices.Length; i++)
        {
            int di = indices[i];
            if (di < 0)
                continue;
            float spread = (k - (total - 1) * 0.5f) * 0.14f;
            k++;
            Entity drop = EntityFactory.CreateDropItem(di, x + spread, y);
            if (!drop.IsNull)
                EntityManager.AddComponent(drop, new CPoolGetTag());
        }
    }

    void SpawnDropsFromTwoIndexArrays(float x, float y, int[] first, int[] second)
    {
        int total = CountPositiveDropIndices(first) + CountPositiveDropIndices(second);
        if (total == 0)
            return;
        int k = 0;
        k = EmitDropIndexArray(x, y, first, k, total);
        EmitDropIndexArray(x, y, second, k, total);
    }

    int EmitDropIndexArray(float x, float y, int[] indices, int k, int total)
    {
        if (indices == null)
            return k;
        for (int i = 0; i < indices.Length; i++)
        {
            int di = indices[i];
            if (di < 0)
                continue;
            float spread = (k - (total - 1) * 0.5f) * 0.14f;
            k++;
            Entity drop = EntityFactory.CreateDropItem(di, x + spread, y);
            if (!drop.IsNull)
                EntityManager.AddComponent(drop, new CPoolGetTag());
        }
        return k;
    }

    void TryApplyDropPickup(Entity dropEntity, Entity playerEntity, Span<CCollider> colliders)
    {
        if (!EntityManager.IsValid(dropEntity) || !EntityManager.IsValid(playerEntity))
            return;
        if (!EntityManager.HasComponent<CDropItem>(dropEntity) || !EntityManager.HasComponent<CPlayer>(playerEntity))
            return;

        int di = dropEntity.Index;
        ref readonly var dropCol = ref colliders[di];
        if (dropCol.layer != E_ColliderLayer.Item)
            return;

        if (TempBitSets.DropItemPickupConsumed.Get(di))
            return;

        ref readonly var dropComp = ref EntityManager.GetComponentSpan<CDropItem>()[di];
        var cfg = GameResDB.Instance.GetConfig<DropItemConfig>(dropComp.cfgIndex);
        if (cfg == null)
            return;

        DropItemPickupEffects.Apply(in cfg, EntityManager, playerEntity);

        TempBitSets.DropItemPickupConsumed.Set(di, true);
        EntityManager.AddComponent(di, new CPoolRecycleTag());
    }
}
