using System;

/// <summary>
/// 被 <see cref="CDropItemMagnet"/> 标记的掉落物飞向玩家，到达后拾取。
/// 须在 <see cref="DropItemCollectSystem"/> 之后运行。
/// </summary>
public class DropItemMagnetSystem : BaseSystem
{
    public override void OnLogicTick(uint currentFrame)
    {
        if (!GlobalBattleData.IsInitialized)
            return;

        Span<int> indices = EntityManager.GetActiveIndices<CDropItemMagnet>();
        if (indices.Length == 0)
            return;

        DropItemCollectData collect = GlobalBattleData.DropItemCollectData;
        uint logicFps = GameManager.logicFPS > 0 ? GameManager.logicFPS : 60;
        float speedPerFrame = collect.ResolveMagnetSpeedPerSecond() / logicFps;
        float pickupRadius = collect.ResolveMagnetPickupRadius();

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();

        for (int i = 0; i < indices.Length; i++)
        {
            int dropIdx = indices[i];
            Entity drop = EntityManager.GetEntity(dropIdx);
            if (!EntityManager.IsValid(drop) || !EntityManager.HasComponent<CDropItem>(drop))
                continue;

            if (TempBitSets.DropItemPickupConsumed.Get(dropIdx))
                continue;

            ref readonly var magnet = ref EntityManager.GetComponent<CDropItemMagnet>(drop);
            int targetIdx = magnet.targetPlayerEntityIndex;
            Entity targetPlayer = EntityManager.GetEntity(targetIdx);
            if (!EntityManager.IsValid(targetPlayer) || !EntityManager.HasComponent<CPlayer>(targetPlayer))
            {
                EntityManager.RemoveComponent<CDropItemMagnet>(drop);
                continue;
            }

            ref readonly var targetPos = ref positions[targetIdx];
            ref var dropPos = ref positions[dropIdx];

            DropItemMagnetSimulator.StepTowardTarget(
                ref dropPos,
                targetPos.x,
                targetPos.y,
                speedPerFrame,
                pickupRadius,
                out bool reached);

            if (dropIdx < rotations.Length)
                rotations[dropIdx].angleRad = 0f;

            if (!reached)
                continue;

            DropItemPickup.ApplyPickupEffects(EntityManager, drop, targetPlayer);
            if (DropItemPickup.TryConsumeDrop(EntityManager, drop))
                EntityManager.RemoveComponent<CDropItemMagnet>(drop);
        }
    }
}
