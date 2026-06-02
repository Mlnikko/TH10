using UnityEngine;

/// <summary>道中波次出怪时应用队列条目上的发射器覆盖（留空则用 <see cref="EnemyConfig.emitterConfigId"/>）。</summary>
public static class EnemyWaveSpawnEmitter
{
    public static void ApplyOverride(EntityManager em, Entity entity, int emitterConfigIndex)
    {
        if (emitterConfigIndex < 0)
            return;

        if (em.HasComponent<CDanmakuEmitter>(entity))
            em.RemoveComponent<CDanmakuEmitter>(entity);

        var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterConfigIndex);
        if (emitterCfg == null)
            return;

        em.AddComponent(entity, new CDanmakuEmitter(emitterCfg)
        {
            isEmitting = true,
            randomSeed = (uint)((entity.Index + 1) * 3266489917u),
        });
    }
}
