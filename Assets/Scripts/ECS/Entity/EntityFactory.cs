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

        foreach (var emitterIndex in weaponConfig.danmakuEmitterCfgIndices)
        {
            var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterIndex);
            if (emitterCfg == null)
            {
                Logger.Error($"DanmakuEmitter configuration not found for index {emitterIndex}.");
                return Entity.Null;
            }
            _entityManager.AddComponent(e_player, new CDanmakuEmitter(emitterCfg));
        }

        Logger.Info($"Player {playerBattleData.playerIndex} ({playerBattleData.characterId}) initialized successfully.", LogTag.Battle);
        return e_player;
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
