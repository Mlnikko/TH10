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

        string characterId = playerBattleData.characterId.ToString().ToLowerInvariant();
        string weaponId = playerBattleData.weaponId.ToString().ToLowerInvariant();

        var characterConfig = GameResDB.Instance.GetConfig<CharacterConfig>(characterId);
        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(weaponId);

        if (characterConfig == null)
        {
            Logger.Error($"CharacterConfig not found for Key: {characterId}");
            return Entity.Null;
        }

        GameObjectPoolManager.Instance.WarmupPool(characterConfig.characterPrefabIndex, 1);

        _entityManager.AddComponent(e_player, new CPosition(posX, posY));
        _entityManager.AddComponent(e_player, new CVelocity(0, 0));
        _entityManager.AddComponent(e_player, new CPlayer()
        {
            playerIndex = playerBattleData.playerIndex,
            characterCfgIndex = (byte)playerBattleData.characterId,
            weaponCfgIndex = (byte)playerBattleData.weaponId,

            moveSpeed = characterConfig.moveSpeed,
            moveSlowSpeed = characterConfig.moveSlowSpeed,

            hitRadius = characterConfig.hitColliderConfig.radius,
            grazeRadius = characterConfig.grazeColliderConfig.radius,

            isShooting = false,
            isSlowMode = false,
            isBombing = false,
            isInvincible = false,
        });
        _entityManager.AddComponent(e_player, new CHealth(characterConfig.maxHealth, characterConfig.maxHealth));

        foreach (var emitterIndex in weaponConfig.danmakuEmitterCfgIndices)
        {
            var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterIndex);
            if (emitterCfg == null)
            {
                Logger.Error($"DanmakuEmitter configuration not found for index {emitterIndex}.");
                return Entity.Null;
            }

            _entityManager.AddComponent(e_player, new CPosition(posX, posY));
            _entityManager.AddComponent(e_player, new CRotation(0));
            _entityManager.AddComponent(e_player, new CVelocity(0, 0));
            _entityManager.AddComponent(e_player, new CDanmakuEmitter(emitterCfg));
        }

        Logger.Info($"Player {playerBattleData.playerIndex} ({playerBattleData.characterId}) initialized successfully.", LogTag.Battle);
        return e_player;
    }

    public Entity CreateDanmaku(float posX, float posY, float rotZ, float velX, float velY, int danmakuCfgIndex)
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
        _entityManager.AddComponent(e_danmaku, new CRotation(rotZ));
        _entityManager.AddComponent(e_danmaku, new CVelocity(velX, velY));
        _entityManager.AddComponent(e_danmaku, new CCollider(
            true,
            danmakuCfg.colliderConfig.shape,
            danmakuCfg.colliderConfig.layer,
            danmakuCfg.colliderConfig.mask,
            danmakuCfg.colliderConfig.offset.x,
            danmakuCfg.colliderConfig.offset.y,
            danmakuCfg.colliderConfig.radius,
            danmakuCfg.colliderConfig.boxSize.x,
            danmakuCfg.colliderConfig.boxSize.y
            ));

        return e_danmaku;
    }

    public Entity CreateEnemy(EnemyConfig enemyConfig, float posX, float posY)
    {
        Entity e_enemy = _entityManager.CreateEntity();
        _entityManager.AddComponent(e_enemy, new CEnemy{enemyCfgIndex = enemyConfig.emitterConfigIndex, currentHealth = enemyConfig.maxHealth});
        _entityManager.AddComponent(e_enemy, new CPosition(posX, posY));
        _entityManager.AddComponent(e_enemy, new CVelocity(0, 0));
        _entityManager.AddComponent(e_enemy, new CCollider
        {
            isActive = true,
            isDirty = false,
            type = enemyConfig.colliderConfig.shape,
            layer = enemyConfig.colliderConfig.layer,
            mask = enemyConfig.colliderConfig.mask,
            offsetX = enemyConfig.colliderConfig.offset.x,
            offsetY = enemyConfig.colliderConfig.offset.y,
            radius = enemyConfig.colliderConfig.radius,         
            width = enemyConfig.colliderConfig.boxSize.x,
            height = enemyConfig.colliderConfig.boxSize.y,
        });
        return e_enemy;
    }
}
