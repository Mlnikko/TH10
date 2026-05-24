using System;

/// <summary>
/// 玩家进入战斗区上方道具吸收线后，为全场掉落物附加飞向玩家的磁吸标记。
/// 须在 <see cref="PlayerControlSystem"/> 之后运行，以便本帧位移已生效。
/// </summary>
public class DropItemCollectSystem : BaseSystem
{
    public override void OnLogicTick(uint currentFrame)
    {
        if (!GlobalBattleData.IsInitialized)
            return;

        BattleAreaData area = GlobalBattleData.AreaData;
        DropItemCollectData collect = GlobalBattleData.DropItemCollectData;

        Span<int> playerIndices = EntityManager.GetActiveIndices<CPlayer>();
        if (playerIndices.Length == 0)
            return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var players = EntityManager.GetComponentSpan<CPlayer>();

        Span<int> collectors = stackalloc int[4];
        int collectorCount = 0;

        for (int i = 0; i < playerIndices.Length; i++)
        {
            int idx = playerIndices[i];
            if ((uint)idx >= (uint)positions.Length)
                continue;

            ref readonly var pos = ref positions[idx];
            if (!collect.IsInCollectZone(pos.y, in area))
                continue;

            InsertCollectorSorted(collectors, ref collectorCount, idx, players);
        }

        if (collectorCount == 0)
            return;

        Span<int> dropIndices = EntityManager.GetActiveIndices<CDropItem>();
        for (int i = 0; i < dropIndices.Length; i++)
        {
            int di = dropIndices[i];
            Entity drop = EntityManager.GetEntity(di);
            if (!EntityManager.IsValid(drop))
                continue;

            if (TempBitSets.DropItemPickupConsumed.Get(di))
                continue;

            if (EntityManager.HasComponent<CDropItemMagnet>(drop))
                continue;

            int targetPlayerIdx = FindNearestCollectorIndex(di, collectors, collectorCount, positions);
            EntityManager.AddComponent(drop, new CDropItemMagnet(targetPlayerIdx));
        }
    }

    static int FindNearestCollectorIndex(
        int dropEntityIndex,
        Span<int> collectors,
        int collectorCount,
        Span<CPosition> positions)
    {
        ref readonly var dropPos = ref positions[dropEntityIndex];
        int best = collectors[0];
        float bestDistSq = float.MaxValue;

        for (int c = 0; c < collectorCount; c++)
        {
            int playerIdx = collectors[c];
            float dx = positions[playerIdx].x - dropPos.x;
            float dy = positions[playerIdx].y - dropPos.y;
            float distSq = dx * dx + dy * dy;

            if (distSq < bestDistSq || (distSq == bestDistSq && playerIdx < best))
            {
                bestDistSq = distSq;
                best = playerIdx;
            }
        }

        return best;
    }

    static void InsertCollectorSorted(
        Span<int> buffer,
        ref int count,
        int playerEntityIndex,
        Span<CPlayer> players)
    {
        if (count >= buffer.Length)
            return;

        byte playerIndex = players[playerEntityIndex].playerIndex;
        int insertAt = count;
        for (int i = 0; i < count; i++)
        {
            if (players[buffer[i]].playerIndex > playerIndex)
            {
                insertAt = i;
                break;
            }
        }

        for (int i = count; i > insertAt; i--)
            buffer[i] = buffer[i - 1];

        buffer[insertAt] = playerEntityIndex;
        count++;
    }
}
