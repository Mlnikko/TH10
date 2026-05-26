using System;
using UnityEngine;

/// <summary>
/// 处理受击后的复活倒计时，并在出生点重建玩家实体。
/// </summary>
public class PlayerRespawnSystem : BaseSystem
{
    public override void OnLogicTick(uint currentframe)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CPlayerRespawnPending>();
        if (indices.Length == 0)
            return;

        var pendings = EntityManager.GetComponentSpan<CPlayerRespawnPending>();
        int totalPlayers = BattleManager.Instance != null ? BattleManager.Instance.TotalPlayerCount : 1;

        for (int i = 0; i < indices.Length; i++)
        {
            int entityIdx = indices[i];
            ref var pending = ref pendings[entityIdx];

            if (pending.framesUntilSpawn > 0)
            {
                pending.framesUntilSpawn--;
                continue;
            }

            SpawnPlayer(ref pending);
            EntityManager.DestroyEntity(EntityManager.GetEntity(entityIdx));
        }
    }

    void SpawnPlayer(ref CPlayerRespawnPending pending)
    {
        if (!BattleManager.Instance.TryGetPlayerBattleData(pending.playerIndex, out var battleData))
        {
            Logger.Warn($"[PlayerRespawn] 找不到 playerIndex={pending.playerIndex} 的会话数据。", LogTag.Battle);
            return;
        }

        Entity playerEntity = EntityFactory.CreatePlayer(battleData, pending.spawnX, pending.spawnY);
        if (playerEntity.IsNull)
            return;

        ref var player = ref EntityManager.GetComponent<CPlayer>(playerEntity);
        player.powerOrbs = 0;
        player.invincibleFramesRemaining = pending.invincibleFramesAfterSpawn;

        ref var hp = ref EntityManager.GetComponent<CHealth>(playerEntity);
        hp.currentHealth = pending.remainingHealth;
        hp.maxHealth = Math.Max(hp.maxHealth, pending.remainingHealth);

        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig != null)
            EntityFactory.SyncPlayerWeaponPowerLayouts(playerEntity, weaponConfig, 0);

        EntityManager.AddComponent(playerEntity, new CPoolGetTag());
    }
}
