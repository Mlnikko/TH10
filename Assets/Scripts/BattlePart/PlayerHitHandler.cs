using System;
using UnityEngine;

/// <summary>
/// 玩家与敌人 / 敌弹碰撞后的受击、散落 Power、摧毁与复活排队（确定性逻辑）。
/// </summary>
public static class PlayerHitHandler
{
    const int MaxPowerDropCount = 24;

    public static void TryApplyHit(
        EntityManager em,
        EntityFactory factory,
        Entity playerEntity,
        Entity hazardEntity,
        Span<CCollider> colliders,
        int totalPlayers)
    {
        if (!em.IsValid(playerEntity) || !em.HasComponent<CPlayer>(playerEntity))
            return;

        int pi = playerEntity.Index;
        if (TempBitSets.PlayerHitConsumed.Get(pi))
            return;

        if (em.HasComponent<CPoolRecycleTag>(playerEntity))
            return;

        ref var player = ref em.GetComponent<CPlayer>(playerEntity);
        if (player.isInvincible || player.invincibleFramesRemaining > 0)
            return;

        if (!IsPlayerHazardPair(em, playerEntity, hazardEntity, colliders))
            return;

        if (!em.HasComponent<CHealth>(playerEntity))
            return;

        ref var hp = ref em.GetComponent<CHealth>(playerEntity);
        if (hp.currentHealth <= 0)
            return;

        TempBitSets.PlayerHitConsumed.Set(pi, true);

        ref readonly var pos = ref em.GetComponent<CPosition>(playerEntity);
        var characterCfg = GameResDB.Instance.GetConfig<CharacterConfig>(player.characterCfgIndex);

        hp.currentHealth--;

        SpawnDeathPowerDrops(em, factory, pos.x, pos.y, player.powerOrbs, characterCfg);

        if (em.HasComponent<CCollider>(playerEntity))
        {
            ref var col = ref colliders[pi];
            col.isActive = false;
        }

        factory.DestroyPlayerWeaponEmitters(pi);
        em.AddComponent(playerEntity, new CPoolRecycleTag());

        if (hp.currentHealth > 0)
        {
            QueueRespawn(em, ref player, hp.currentHealth, characterCfg, totalPlayers);
        }
        else
        {
            Logger.Info($"Player {player.playerIndex} ran out of lives.", LogTag.Battle);
            BattleManager.Instance?.NotifyPlayerEliminated(player.playerIndex);
        }
    }

    static bool IsPlayerHazardPair(
        EntityManager em,
        Entity playerEntity,
        Entity hazardEntity,
        Span<CCollider> colliders)
    {
        if (!em.IsValid(hazardEntity))
            return false;

        int hi = hazardEntity.Index;
        if ((uint)hi >= (uint)colliders.Length)
            return false;

        ref readonly var playerCol = ref colliders[playerEntity.Index];
        ref readonly var hazardCol = ref colliders[hi];

        if (playerCol.layer != E_ColliderLayer.Player)
            return false;

        if (em.HasComponent<CEnemy>(hazardEntity))
            return hazardCol.layer == E_ColliderLayer.Enemy;

        if (em.HasComponent<CDanmaku>(hazardEntity))
            return hazardCol.layer == E_ColliderLayer.EnemyDanmaku;

        return false;
    }

    static void SpawnDeathPowerDrops(
        EntityManager em,
        EntityFactory factory,
        float x,
        float y,
        int powerOrbs,
        CharacterConfig characterCfg)
    {
        if (powerOrbs <= 0 || characterCfg == null || characterCfg.deathPowerDropCfgIndex < 0)
            return;

        int count = Math.Min(MaxPowerDropCount, (powerOrbs + 9) / 10);
        for (int i = 0; i < count; i++)
        {
            float spread = (i - (count - 1) * 0.5f) * 0.14f;
            Entity drop = factory.CreateDropItem(characterCfg.deathPowerDropCfgIndex, x + spread, y);
            if (!drop.IsNull)
                em.AddComponent(drop, new CPoolGetTag());
        }
    }

    static void QueueRespawn(
        EntityManager em,
        ref CPlayer player,
        int remainingHealth,
        CharacterConfig characterCfg,
        int totalPlayers)
    {
        Span<int> pendingIndices = em.GetActiveIndices<CPlayerRespawnPending>();
        var pendings = em.GetComponentSpan<CPlayerRespawnPending>();
        for (int i = 0; i < pendingIndices.Length; i++)
        {
            if (pendings[pendingIndices[i]].playerIndex == player.playerIndex)
                return;
        }

        int delayFrames = characterCfg != null && characterCfg.deathRespawnDelayFrames > 0
            ? characterCfg.deathRespawnDelayFrames
            : 30;
        int invincibleFrames = characterCfg != null && characterCfg.postHitInvincibleFrames > 0
            ? characterCfg.postHitInvincibleFrames
            : 120;

        var spawnPos = GlobalBattleData.SpawnData.GetPlayerSpawnPos(player.playerIndex, totalPlayers);

        Entity pending = em.CreateEntity();
        em.AddComponent(pending, new CPlayerRespawnPending
        {
            playerIndex = player.playerIndex,
            characterCfgIndex = player.characterCfgIndex,
            weaponCfgIndex = player.weaponCfgIndex,
            remainingHealth = remainingHealth,
            invincibleFramesAfterSpawn = invincibleFrames,
            framesUntilSpawn = delayFrames,
            spawnX = spawnPos.x,
            spawnY = spawnPos.y,
        });
    }
}
