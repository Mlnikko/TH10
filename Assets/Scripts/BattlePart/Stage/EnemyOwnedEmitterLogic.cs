using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人/Boss 附属弹幕发射子实体：跟随宿主位姿，符卡阶段可挂多个 <see cref="CDanmakuEmitter"/>。
/// </summary>
public static class EnemyOwnedEmitterLogic
{
    public static void SyncOwnedEmitters(EntityManager em)
    {
        Span<int> ownedIndices = em.GetActiveIndices<CEnemyEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = em.GetComponentSpan<CEnemyEmitterOwnership>();
        var positions = em.GetComponentSpan<CPosition>();
        var rotations = em.GetComponentSpan<CRotation>();

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref readonly var ownership = ref ownerships[emitterIdx];
            int ownerIdx = ownership.ownerEnemyEntityIndex;
            if (ownerIdx < 0 || ownerIdx >= positions.Length)
                continue;
            if (!em.HasComponent<CPosition>(ownerIdx) || !em.HasComponent<CRotation>(ownerIdx))
                continue;

            ref readonly var ownerPos = ref positions[ownerIdx];
            ref readonly var ownerRot = ref rotations[ownerIdx];
            positions[emitterIdx] = ownerPos;
            rotations[emitterIdx] = ownerRot;
        }
    }

    public static void ClearOwnedEmitters(EntityManager em, int ownerEnemyEntityIndex)
    {
        Span<int> ownedIndices = em.GetActiveIndices<CEnemyEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = em.GetComponentSpan<CEnemyEmitterOwnership>();
        var toDestroy = new List<Entity>(4);

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            if (ownerships[emitterIdx].ownerEnemyEntityIndex != ownerEnemyEntityIndex)
                continue;

            toDestroy.Add(em.GetEntity(emitterIdx));
        }

        for (int i = 0; i < toDestroy.Count; i++)
            em.DestroyEntity(toDestroy[i]);
    }

    public static void SpawnOwnedEmitter(EntityManager em, int ownerEnemyEntityIndex, int emitterCfgIndex, int saltIndex = 0)
    {
        if (emitterCfgIndex < 0)
            return;

        var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterCfgIndex);
        if (emitterCfg == null)
            return;

        Entity ownerEntity = em.GetEntity(ownerEnemyEntityIndex);
        if (!em.IsValid(ownerEntity)
            || !em.HasComponent<CPosition>(ownerEntity)
            || !em.HasComponent<CRotation>(ownerEntity))
        {
            return;
        }

        ref readonly var ownerPos = ref em.GetComponent<CPosition>(ownerEntity);
        ref readonly var ownerRot = ref em.GetComponent<CRotation>(ownerEntity);

        Entity eEmitter = em.CreateEntity();
        em.AddComponent(eEmitter, new CPosition(ownerPos.x, ownerPos.y));
        em.AddComponent(eEmitter, new CRotation(ownerRot.angleRad));

        var emitter = new CDanmakuEmitter(emitterCfg)
        {
            isEmitting = true,
            randomSeed = (uint)((ownerEnemyEntityIndex + 1) * 3266489917u + (uint)(saltIndex + 1) * 668265263u),
        };
        em.AddComponent(eEmitter, emitter);
        em.AddComponent(eEmitter, new CEnemyEmitterOwnership
        {
            ownerEnemyEntityIndex = ownerEnemyEntityIndex,
        });
    }

    public static void ApplySpellEmitters(EntityManager em, Entity bossEntity, BossPhaseConfig phase)
    {
        if (!em.IsValid(bossEntity) || phase == null)
            return;

        ClearOwnedEmitters(em, bossEntity.Index);

        if (em.HasComponent<CDanmakuEmitter>(bossEntity))
            em.RemoveComponent<CDanmakuEmitter>(bossEntity);

        int spawned = 0;
        if (phase.spellEmitters != null && phase.spellEmitters.Length > 0)
        {
            for (int i = 0; i < phase.spellEmitters.Length; i++)
            {
                int cfgIndex = phase.spellEmitters[i].emitterConfigIndex;
                if (cfgIndex < 0)
                    continue;

                SpawnOwnedEmitter(em, bossEntity.Index, cfgIndex, i);
                spawned++;
            }
        }
        else if (phase.spellCardEmitterIndex >= 0)
        {
            SpawnOwnedEmitter(em, bossEntity.Index, phase.spellCardEmitterIndex, 0);
            spawned++;
        }

        if (spawned == 0)
        {
            Logger.Warn(
                $"[EnemyOwnedEmitterLogic] Boss phase has no valid spell emitters (phase: {phase.ConfigId}).",
                LogTag.Resource);
        }
    }
}
