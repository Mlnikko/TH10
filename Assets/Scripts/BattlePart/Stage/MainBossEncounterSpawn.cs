using UnityEngine;

/// <summary>关底 Boss 生成时挂载登场路径与 <see cref="CMainBossEncounter"/>。</summary>
public static class MainBossEncounterSpawn
{
    public static void ApplyToEntity(
        EntityManager em,
        Entity entity,
        MainBossEncounterConfig encounter,
        uint currentFrame,
        float spawnX,
        float spawnY)
    {
        bool hasEntry = encounter.entryPathRouteBakeIndex >= 0;
        var pathPhase = hasEntry ? E_MainBossPathPhase.Entry : E_MainBossPathPhase.Loop;

        int cfgIndex = GameResDB.Instance.GetConfigIndex(encounter.ConfigId);
        em.AddComponent(entity, new CMainBossEncounter
        {
            pathPhase = pathPhase,
            encounterCfgIndex = cfgIndex,
            entryRouteBakeIndex = encounter.entryPathRouteBakeIndex,
            loopRouteBakeIndex = encounter.loopPathRouteBakeIndex,
            entryDurationFrames = encounter.entryDurationFrames,
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
    }
}
