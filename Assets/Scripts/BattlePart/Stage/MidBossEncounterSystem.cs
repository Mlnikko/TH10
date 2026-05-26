using System;
using UnityEngine;

/// <summary>
/// 中场 Boss 阶段机：入场 → 场内循环 → 退场；在 <see cref="EnemyMovementSystem"/> 之前更新 <see cref="CEnemyPathMovement"/>。
/// </summary>
public class MidBossEncounterSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CMidBossEncounter>();
        if (indices.Length == 0)
            return;

        var mids = EntityManager.GetComponentSpan<CMidBossEncounter>();
        var paths = EntityManager.GetComponentSpan<CEnemyPathMovement>();
        var positions = EntityManager.GetComponentSpan<CPosition>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            ref var mid = ref mids[idx];
            if (mid.phase == E_MidBossPhase.Done)
                continue;

            Entity entity = EntityManager.GetEntity(idx);
            if (!EntityManager.IsValid(entity))
                continue;

            if (!EntityManager.HasComponent<CEnemyPathMovement>(entity))
            {
                if (mid.phase == E_MidBossPhase.Exit)
                    FinishExit(entity, ref mid, frame);
                continue;
            }

            ref var path = ref paths[idx];
            ref var pos = ref positions[idx];
            uint pathAge = frame - path.spawnFrame;

            switch (mid.phase)
            {
                case E_MidBossPhase.Entry:
                    if (mid.entryDurationFrames <= 0 || pathAge >= (uint)mid.entryDurationFrames)
                        TransitionToOnField(entity, ref mid, ref path, ref pos, frame);
                    break;

                case E_MidBossPhase.OnField:
                    if (frame >= mid.onFieldEndFrame)
                        TransitionToExit(entity, ref mid, ref path, ref pos, frame);
                    break;

                case E_MidBossPhase.Exit:
                    if (mid.exitDurationFrames <= 0 || pathAge >= (uint)mid.exitDurationFrames)
                        FinishExit(entity, ref mid, frame);
                    break;
            }
        }
    }

    void TransitionToOnField(
        Entity entity,
        ref CMidBossEncounter mid,
        ref CEnemyPathMovement path,
        ref CPosition pos,
        uint frame)
    {
        mid.phase = E_MidBossPhase.OnField;
        mid.phaseStartFrame = frame;

        if (mid.loopRouteBakeIndex < 0)
        {
            EntityManager.RemoveComponent<CEnemyPathMovement>(entity);
            return;
        }

        path.spawnFrame = frame;
        path.originX = pos.x;
        path.originY = pos.y;
        path.routeBakeIndex = mid.loopRouteBakeIndex;
        path.loopRoute = true;
    }

    void TransitionToExit(
        Entity entity,
        ref CMidBossEncounter mid,
        ref CEnemyPathMovement path,
        ref CPosition pos,
        uint frame)
    {
        mid.phase = E_MidBossPhase.Exit;
        mid.phaseStartFrame = frame;

        if (mid.exitRouteBakeIndex < 0)
        {
            FinishExit(entity, ref mid, frame);
            return;
        }

        path.spawnFrame = frame;
        path.originX = pos.x;
        path.originY = pos.y;
        path.routeBakeIndex = mid.exitRouteBakeIndex;
        path.loopRoute = false;
    }

    void FinishExit(Entity entity, ref CMidBossEncounter mid, uint frame)
    {
        mid.phase = E_MidBossPhase.Done;
        mid.phaseStartFrame = frame;
        if (!EntityManager.HasComponent<CPoolRecycleTag>(entity))
            EntityManager.AddComponent(entity, new CPoolRecycleTag());
    }
}
