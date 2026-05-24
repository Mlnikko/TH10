using System;
using UnityEngine;

public class EntityFactory
{
    readonly EntityManager _entityManager;

    public EntityFactory(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }
   
    public Entity CreatePlayer(PlayerBattleData playerBattleData, float posX, float posY)
    {
        Entity e_player = _entityManager.CreateEntity();

        string characterId = StringHelper.NormalizeResourceId(playerBattleData.characterId.ToString());
        string weaponId = StringHelper.NormalizeResourceId(playerBattleData.weaponId.ToString());

        var characterConfig = GameResDB.Instance.GetConfig<CharacterConfig>(characterId);
        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(weaponId);

        if (characterConfig == null)
        {
            Logger.Error($"CharacterConfig not found for Key: {characterId}");
            return Entity.Null;
        }

        if (weaponConfig == null)
        {
            Logger.Error($"WeaponConfig not found for Key: {weaponId}");
            return Entity.Null;
        }

        int characterCfgIndex = GameResDB.Instance.GetConfigIndex(characterId);
        int weaponCfgIndex = GameResDB.Instance.GetConfigIndex(weaponId);
        if (characterCfgIndex < 0 || characterCfgIndex > byte.MaxValue)
        {
            Logger.Error($"CharacterConfig index invalid for Key: {characterId} (index={characterCfgIndex})");
            return Entity.Null;
        }
        if (weaponCfgIndex < 0 || weaponCfgIndex > byte.MaxValue)
        {
            Logger.Error($"WeaponConfig index invalid for Key: {weaponId} (index={weaponCfgIndex})");
            return Entity.Null;
        }

        _entityManager.AddComponent(e_player, new CPosition(posX, posY));
        _entityManager.AddComponent(e_player, new CRotation(0));
        _entityManager.AddComponent(e_player, new CVelocity(0, 0));
        _entityManager.AddComponent(e_player, new CPlayer()
        {
            playerIndex = playerBattleData.playerIndex,
            characterCfgIndex = (byte)characterCfgIndex,
            weaponCfgIndex = (byte)weaponCfgIndex,

            moveDistancePerFrame = characterConfig.moveDistancePerFrame,
            moveSlowDistancePerFrame = characterConfig.moveSlowDistancePerFrame,

            hitRadius = characterConfig.hitColliderConfig.radius,
            grazeRadius = characterConfig.grazeColliderConfig.radius,

            isShooting = false,
            isSlowMode = false,
            isBombing = false,
            isInvincible = false,
            powerOrbs = 0,
            primaryEmitterEntityIndex = -1,
            emitterSlotLayoutVariant = 0,
            appliedSecondaryPowerMinOrbs = int.MinValue,
        });
        _entityManager.AddComponent(e_player, new CHealth(characterConfig.maxHealth, characterConfig.maxHealth));
        _entityManager.AddComponent(e_player, new CCollider
        {
            isActive = true,
            shape = characterConfig.hitColliderConfig.shape,
            layer = characterConfig.hitColliderConfig.layer,
            mask = characterConfig.hitColliderConfig.mask,
            offsetX = characterConfig.hitColliderConfig.offset.x,
            offsetY = characterConfig.hitColliderConfig.offset.y,
            radius = characterConfig.hitColliderConfig.radius,
            width = characterConfig.hitColliderConfig.boxSize.x,
            height = characterConfig.hitColliderConfig.boxSize.y,
        });

        int playerEntityIndex = e_player.Index;
        ref var playerComponent = ref _entityManager.GetComponent<CPlayer>(e_player);
        int primaryEmitterEntityIndex = CreatePlayerWeaponEmitters(
            playerEntityIndex,
            weaponConfig,
            playerComponent.powerOrbs,
            posX,
            posY);

        playerComponent.primaryEmitterEntityIndex = primaryEmitterEntityIndex;
        playerComponent.appliedSecondaryPowerMinOrbs =
            ResolveSecondaryPowerTierKey(weaponConfig, playerComponent.powerOrbs);

        Logger.Info($"Player {playerBattleData.playerIndex} ({playerBattleData.characterId}) initialized successfully.", LogTag.Battle);
        return e_player;
    }

    int CreatePlayerWeaponEmitters(
        int ownerPlayerEntityIndex,
        WeaponConfig weaponConfig,
        int powerOrbs,
        float posX,
        float posY)
    {
        int primaryEmitterEntityIndex = -1;

        int primaryCfgIndex = weaponConfig.ResolvePrimaryEmitterCfgIndex(slowMode: false);
        if (primaryCfgIndex >= 0)
        {
            primaryEmitterEntityIndex = CreateWeaponEmitterEntity(
                ownerPlayerEntityIndex,
                primaryCfgIndex,
                weaponConfig.primaryEmitters.normal.slotOffset,
                E_WeaponEmitterSlotRole.Primary,
                secondarySlotIndex: 0,
                posX,
                posY);
        }

        SpawnPlayerSecondaryEmitters(ownerPlayerEntityIndex, weaponConfig, powerOrbs, posX, posY);
        return primaryEmitterEntityIndex;
    }

    /// <summary>按当前 Power 重建玩家副发射器实体（主炮不变）。</summary>
    public void SyncPlayerSecondaryEmitters(Entity playerEntity, WeaponConfig weaponConfig, int powerOrbs)
    {
        if (!_entityManager.IsValid(playerEntity) || weaponConfig == null)
            return;

        int tierKey = ResolveSecondaryPowerTierKey(weaponConfig, powerOrbs);

        ref var player = ref _entityManager.GetComponent<CPlayer>(playerEntity);
        if (tierKey == player.appliedSecondaryPowerMinOrbs)
            return;

        player.appliedSecondaryPowerMinOrbs = tierKey;

        DestroyPlayerSecondaryEmitters(playerEntity.Index);

        ref var pos = ref _entityManager.GetComponent<CPosition>(playerEntity);
        SpawnPlayerSecondaryEmitters(playerEntity.Index, weaponConfig, powerOrbs, pos.x, pos.y);
    }

    static int ResolveSecondaryPowerTierKey(WeaponConfig weaponConfig, int powerOrbs)
    {
        return weaponConfig.TryResolvePowerSecondary(powerOrbs, out var tier)
            ? tier.minPowerOrbs
            : int.MinValue;
    }

    void SpawnPlayerSecondaryEmitters(
        int ownerPlayerEntityIndex,
        WeaponConfig weaponConfig,
        int powerOrbs,
        float posX,
        float posY)
    {
        if (!weaponConfig.TryResolvePowerSecondary(powerOrbs, out var tier))
            return;

        var indices = tier.emitterCfgIndices;
        var slots = tier.slots;
        if (indices == null || slots == null)
            return;

        int count = Mathf.Min(indices.Length, slots.Length);
        for (int i = 0; i < count; i++)
        {
            if (indices[i] < 0)
                continue;

            CreateWeaponEmitterEntity(
                ownerPlayerEntityIndex,
                indices[i],
                slots[i].slotOffset,
                E_WeaponEmitterSlotRole.Secondary,
                (byte)i,
                posX,
                posY);
        }
    }

    void DestroyPlayerSecondaryEmitters(int ownerPlayerEntityIndex)
    {
        Span<int> ownedIndices = _entityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = _entityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        var toDestroy = new System.Collections.Generic.List<Entity>(4);

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != ownerPlayerEntityIndex)
                continue;
            if (ownership.role != E_WeaponEmitterSlotRole.Secondary)
                continue;

            toDestroy.Add(_entityManager.GetEntity(emitterIdx));
        }

        for (int i = 0; i < toDestroy.Count; i++)
            _entityManager.DestroyEntity(toDestroy[i]);
    }

    int CreateWeaponEmitterEntity(
        int ownerPlayerEntityIndex,
        int emitterCfgIndex,
        Vector2 slotOffset,
        E_WeaponEmitterSlotRole role,
        byte secondarySlotIndex,
        float posX,
        float posY)
    {
        var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterCfgIndex);
        if (emitterCfg == null)
        {
            Logger.Error($"DanmakuEmitter configuration not found for index {emitterCfgIndex}.");
            return -1;
        }

        Entity e_emitter = _entityManager.CreateEntity();
        _entityManager.AddComponent(e_emitter, new CPosition(posX, posY));
        _entityManager.AddComponent(e_emitter, new CRotation(0));

        var emitter = new CDanmakuEmitter(emitterCfg);
        emitter.emitterPosOffsetX += slotOffset.x;
        emitter.emitterPosOffsetY += slotOffset.y;
        _entityManager.AddComponent(e_emitter, emitter);
        _entityManager.AddComponent(e_emitter, new CPlayerEmitterOwnership
        {
            ownerPlayerEntityIndex = ownerPlayerEntityIndex,
            role = role,
            secondarySlotIndex = secondarySlotIndex,
            slotOffsetX = slotOffset.x,
            slotOffsetY = slotOffset.y,
        });

        return e_emitter.Index;
    }

    /// <param name="rotationRad">弹幕逻辑旋转（弧度），与 <see cref="CRotation.angleRad"/> 一致。</param>
    public Entity CreateDanmaku(float posX, float posY, float rotationRad, float velX, float velY, int danmakuCfgIndex)
    {
        // 检查配置是否存在
        var danmakuCfg = GameResDB.Instance.GetConfig<DanmakuConfig>(danmakuCfgIndex);

        if (danmakuCfg == null)
        {
            Logger.Error($"Danmaku configuration not found for index {danmakuCfgIndex}.");
            return Entity.Null;
        }

        Entity e_danmaku = _entityManager.CreateEntity();

        _entityManager.AddComponent(e_danmaku, new CDanmaku(danmakuCfgIndex));
        _entityManager.AddComponent(e_danmaku, new CPosition(posX, posY));
        _entityManager.AddComponent(e_danmaku, new CRotation(rotationRad));
        _entityManager.AddComponent(e_danmaku, new CVelocity(velX, velY));
        _entityManager.AddComponent(e_danmaku, new CCollider
        {
            isActive = true,
            shape = danmakuCfg.colliderConfig.shape,
            layer = danmakuCfg.colliderConfig.layer,
            mask = danmakuCfg.colliderConfig.mask,
            offsetX = danmakuCfg.colliderConfig.offset.x,
            offsetY = danmakuCfg.colliderConfig.offset.y,
            radius = danmakuCfg.colliderConfig.radius,
            width = danmakuCfg.colliderConfig.boxSize.x,
            height = danmakuCfg.colliderConfig.boxSize.y
        });

        return e_danmaku;
    }

    public Entity CreateEnemy(EnemyConfig enemyConfig, float posX, float posY, float hpMultiplier = 1f)
    {
        Entity e_enemy = _entityManager.CreateEntity();
        var enemyCfgIndex = GameResDB.Instance.GetConfigIndex(enemyConfig.ConfigId);
        int hp = Mathf.Max(1, Mathf.RoundToInt(enemyConfig.maxHealth * hpMultiplier));
        _entityManager.AddComponent(e_enemy, new CEnemy{
            enemyCfgIndex = enemyCfgIndex,
            currentHealth = hp
            });
        _entityManager.AddComponent(e_enemy, new CPosition(posX, posY));
        _entityManager.AddComponent(e_enemy, new CVelocity(0, 0));
        _entityManager.AddComponent(e_enemy, new CRotation(0));
        _entityManager.AddComponent(e_enemy, new CCollider
        {
            isActive = true,
            shape = enemyConfig.colliderConfig.shape,
            layer = enemyConfig.colliderConfig.layer,
            mask = enemyConfig.colliderConfig.mask,
            offsetX = enemyConfig.colliderConfig.offset.x,
            offsetY = enemyConfig.colliderConfig.offset.y,
            radius = enemyConfig.colliderConfig.radius,         
            width = enemyConfig.colliderConfig.boxSize.x,
            height = enemyConfig.colliderConfig.boxSize.y,
        });

        if (enemyConfig.emitterConfigIndex >= 0)
        {
            var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(enemyConfig.emitterConfigIndex);
            if (emitterCfg != null)
            {
                var emitter = new CDanmakuEmitter(emitterCfg);
                emitter.isEmitting = true;
                _entityManager.AddComponent(e_enemy, emitter);
            }
        }

        return e_enemy;
    }

    /// <summary>
    /// 生成掉落物 ECS 实体（竖直运动见 <see cref="CDropItemMotion"/>）；表现层需另行 <see cref="CPoolGetTag"/>。
    /// </summary>
    public Entity CreateDropItem(int dropCfgIndex, float posX, float posY)
    {
        var cfg = GameResDB.Instance.GetConfig<DropItemConfig>(dropCfgIndex);
        if (cfg == null)
        {
            Logger.Error($"DropItemConfig not found for index {dropCfgIndex}.");
            return Entity.Null;
        }

        Entity e = _entityManager.CreateEntity();
        _entityManager.AddComponent(e, new CDropItem(dropCfgIndex));
        _entityManager.AddComponent(e, new CPosition(posX, posY));
        _entityManager.AddComponent(e, new CRotation(0));
        uint logicFps = GameManager.logicFPS > 0 ? (uint)GameManager.logicFPS : 60;
        _entityManager.AddComponent(e, DropItemMotionSimulator.CreateMotionFromConfig(cfg, logicFps));
        _entityManager.AddComponent(e, new CCollider
        {
            isActive = true,
            shape = cfg.colliderConfig.shape,
            layer = cfg.colliderConfig.layer,
            mask = cfg.colliderConfig.mask,
            offsetX = cfg.colliderConfig.offset.x,
            offsetY = cfg.colliderConfig.offset.y,
            radius = cfg.colliderConfig.radius,
            width = cfg.colliderConfig.boxSize.x,
            height = cfg.colliderConfig.boxSize.y,
        });
        return e;
    }
}
