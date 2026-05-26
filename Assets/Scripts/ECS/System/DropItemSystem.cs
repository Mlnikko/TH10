using System;

/// <summary>
/// 掉落物出场运动（竖直上抛或定向散射后匀速下落）；越界回收。
/// </summary>
public class DropItemSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CDropItem>();
        if (indices.Length == 0)
            return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var motions = EntityManager.GetComponentSpan<CDropItemMotion>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            if (EntityManager.HasComponent<CDropItemMagnet>(EntityManager.GetEntity(idx)))
                continue;

            if ((uint)idx >= (uint)motions.Length)
                continue;

            ref var pos = ref positions[idx];
            ref var motion = ref motions[idx];

            DropItemMotionSimulator.StepMotion(ref motion, out float dx, out float dy, out bool wasRising);
            pos.x += dx;
            pos.y += dy;

            if (idx < rotations.Length)
                DropItemMotionSimulator.StepAscentRotation(wasRising, in motion, ref rotations[idx]);

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
