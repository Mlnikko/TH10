using System;

/// <summary>
/// 关底 Boss 路径：登场路径结束或进入符卡战后切换场内循环路径。
/// 在 <see cref="EnemyMovementSystem"/> 之前运行。
/// </summary>
public class MainBossEncounterSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CMainBossEncounter>();
        if (indices.Length == 0)
            return;

        var bosses = EntityManager.GetComponentSpan<CMainBossEncounter>();
        var paths = EntityManager.GetComponentSpan<CEnemyPathMovement>();
        var positions = EntityManager.GetComponentSpan<CPosition>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            ref var boss = ref bosses[idx];
            if (boss.pathPhase != E_MainBossPathPhase.Entry)
                continue;

            Entity entity = EntityManager.GetEntity(idx);
            if (!EntityManager.IsValid(entity) || !EntityManager.HasComponent<CEnemyPathMovement>(entity))
                continue;

            ref var path = ref paths[idx];
            uint pathAge = frame - path.spawnFrame;
            if (boss.entryDurationFrames > 0 && pathAge < (uint)boss.entryDurationFrames)
                continue;

            TransitionToLoop(EntityManager, entity, ref boss, ref path, ref positions[idx], frame);
        }
    }

    /// <summary>登场对话结束进入符卡战时，若仍在登场路径则切入循环路径。</summary>
    public static void EnsureLoopPathForFight(EntityManager em, Entity entity, MainBossEncounterConfig encounter, uint frame)
    {
        if (encounter == null || !em.IsValid(entity) || !em.HasComponent<CMainBossEncounter>(entity))
            return;

        ref var boss = ref em.GetComponent<CMainBossEncounter>(entity);
        if (boss.pathPhase == E_MainBossPathPhase.Loop)
            return;

        if (!em.HasComponent<CEnemyPathMovement>(entity))
        {
            if (encounter.loopPathRouteBakeIndex < 0)
            {
                boss.pathPhase = E_MainBossPathPhase.Loop;
                return;
            }

            ref var pos = ref em.GetComponent<CPosition>(entity);
            em.AddComponent(entity, new CEnemyPathMovement
            {
                spawnFrame = frame,
                originX = pos.x,
                originY = pos.y,
                routeBakeIndex = encounter.loopPathRouteBakeIndex,
                loopRoute = true,
            });
            boss.pathPhase = E_MainBossPathPhase.Loop;
            return;
        }

        ref var path = ref em.GetComponent<CEnemyPathMovement>(entity);
        ref var position = ref em.GetComponent<CPosition>(entity);
        TransitionToLoop(em, entity, ref boss, ref path, ref position, frame);
    }

    static void TransitionToLoop(
        EntityManager em,
        Entity entity,
        ref CMainBossEncounter boss,
        ref CEnemyPathMovement path,
        ref CPosition pos,
        uint frame)
    {
        boss.pathPhase = E_MainBossPathPhase.Loop;

        if (boss.loopRouteBakeIndex < 0)
        {
            em.RemoveComponent<CEnemyPathMovement>(entity);
            return;
        }

        path.spawnFrame = frame;
        path.originX = pos.x;
        path.originY = pos.y;
        path.routeBakeIndex = boss.loopRouteBakeIndex;
        path.loopRoute = true;
    }
}
