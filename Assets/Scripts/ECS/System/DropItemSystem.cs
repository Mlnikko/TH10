using System;

/// <summary>
/// 掉落物匀速下落（速度来自 <see cref="DropItemConfig"/> 烘焙的每帧位移）与越界回收。
/// </summary>
public class DropItemSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CDropItem>();
        if (indices.Length == 0)
            return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            ref var pos = ref positions[idx];
            ref readonly var vel = ref velocities[idx];

            pos.x += vel.vx;
            pos.y += vel.vy;

            TryRecycleDropOutOfBounds(idx, pos.x, pos.y);
        }
    }

    void TryRecycleDropOutOfBounds(int entityIndex, float x, float y)
    {
        if (!GlobalBattleData.IsInitialized)
            return;

        Entity entity = EntityManager.GetEntity(entityIndex);
        if (!EntityManager.IsValid(entity) || !EntityManager.HasComponent<CDropItem>(entity))
            return;

        if (!GlobalBattleData.AreaData.IsPointInRecycleArea(x, y))
            EntityManager.AddComponent(entityIndex, new CPoolRecycleTag());
    }
}
