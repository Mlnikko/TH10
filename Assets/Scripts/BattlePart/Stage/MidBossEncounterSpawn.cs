using System;
using UnityEngine;

/// <summary>中场 Boss 生成时应用遭遇配置覆盖（血量、发射器、掉落、路径阶段）。</summary>
public static class MidBossEncounterSpawn
{
    public static void ApplyToEntity(
        EntityManager em,
        Entity entity,
        MidBossEncounterConfig encounter,
        EnemyConfig enemyCfg,
        uint currentFrame,
        float spawnX,
        float spawnY)
    {
        if (encounter.emitterConfigIndexOverride >= 0)
        {
            if (em.HasComponent<CDanmakuEmitter>(entity))
                em.RemoveComponent<CDanmakuEmitter>(entity);

            var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(encounter.emitterConfigIndexOverride);
            if (emitterCfg != null)
            {
                var emitter = new CDanmakuEmitter(emitterCfg)
                {
                    isEmitting = true,
                    randomSeed = (uint)((entity.Index + 1) * 3266489917u),
                };
                em.AddComponent(entity, emitter);
            }
        }

        if (encounter.dropOverrideMode != E_WaveDropOverrideMode.UseEnemyConfig)
        {
            em.AddComponent(entity, new CEnemyDeathLoot
            {
                waveDropMode = encounter.dropOverrideMode,
                waveDrops = encounter.dropOnDeathBaked ?? Array.Empty<BakedDeathDropEntry>()
            });
        }

        uint entryEndFrame = currentFrame + (uint)Mathf.Max(0, encounter.entryDurationFrames);
        uint onFieldEnd = entryEndFrame + (uint)encounter.onFieldDurationFrames;

        bool hasEntry = encounter.entryPathRouteBakeIndex >= 0;
        var phase = hasEntry ? E_MidBossPhase.Entry : E_MidBossPhase.OnField;
        if (!hasEntry && encounter.loopPathRouteBakeIndex < 0 && encounter.exitPathRouteBakeIndex < 0)
            onFieldEnd = currentFrame + (uint)encounter.onFieldDurationFrames;

        int cfgIndex = GameResDB.Instance.GetConfigIndex(encounter.ConfigId);
        em.AddComponent(entity, new CMidBossEncounter
        {
            phase = phase,
            phaseStartFrame = currentFrame,
            onFieldEndFrame = onFieldEnd,
            encounterCfgIndex = cfgIndex,
            entryRouteBakeIndex = encounter.entryPathRouteBakeIndex,
            loopRouteBakeIndex = encounter.loopPathRouteBakeIndex,
            exitRouteBakeIndex = encounter.exitPathRouteBakeIndex,
            entryDurationFrames = encounter.entryDurationFrames,
            exitDurationFrames = encounter.exitDurationFrames,
            loopOriginX = spawnX,
            loopOriginY = spawnY,
        });

        if (hasEntry)
        {
            EnemyMovementBaking.TryAttachMovementFromBakeIndex(
                em, entity, encounter.entryPathRouteBakeIndex, currentFrame, spawnX, spawnY);
            return;
        }

        if (encounter.loopPathRouteBakeIndex >= 0)
        {
            em.AddComponent(entity, new CEnemyPathMovement
            {
                spawnFrame = currentFrame,
                originX = spawnX,
                originY = spawnY,
                routeBakeIndex = encounter.loopPathRouteBakeIndex,
                loopRoute = true,
            });
        }

        if (!hasEntry)
            SetLoopOriginOnComponent(em, entity, spawnX, spawnY);
    }

    static void SetLoopOriginOnComponent(EntityManager em, Entity entity, float x, float y)
    {
        if (!em.HasComponent<CMidBossEncounter>(entity))
            return;

        ref var mid = ref em.GetComponent<CMidBossEncounter>(entity);
        mid.loopOriginX = x;
        mid.loopOriginY = y;
    }
}
