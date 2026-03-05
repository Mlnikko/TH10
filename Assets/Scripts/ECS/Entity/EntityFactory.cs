using UnityEngine;

public class EntityFactory
{
    readonly EntityManager _entityManager;
    public EntityFactory(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }
   
    public Entity CreatePlayer(PlayerBattleData playerBattleData, Vector3 pos)
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
        _entityManager.AddComponent(e_player, new CPosition(pos.x, pos.y));
        _entityManager.AddComponent(e_player, new CVelocity(0, 0));
        _entityManager.AddComponent(e_player, new CPlayer(playerBattleData.playerIndex, (byte)playerBattleData.characterId, (byte)playerBattleData.weaponId));
        _entityManager.AddComponent(e_player, new CPlayerRunTime
        {
            isSlowMode = false,
        });

        //_entityManager.AddComponent(e_player, new CPoolGet());


        //battleWorld.GameObjectBridge.Link(playerEntity, playerGO, new PlayerUpdater(playerGO), em);
      

        foreach (var emitterIndex in weaponConfig.danmakuEmitterCfgIndices)
        {
            _entityManager.AddComponent(e_player, new CDanmakuEmitter(true, emitterIndex));
        }


        //_entityManager.AddComponent(e_player, characterConfig.ToRuntimeAttribute(logicTimer.FrameInterval));

        Logger.Info($"Player {playerBattleData.playerIndex} ({playerBattleData.characterId}) initialized successfully.", LogTag.Battle);
        return e_player;
    }

    public Entity CreateDanmaku(DanmakuEmitterConfig emitterConfig, int danmakuCfgIndex, float emitPosX, float emitPosY)
    {
        // ºÏ≤È≈‰÷√ «∑Ò¥Ê‘⁄
        var danmakuCfg = GameResDB.Instance.GetConfig<DanmakuConfig>(danmakuCfgIndex);

        if (danmakuCfg == null)
        {
            Logger.Error($"Danmaku configuration not found for index {danmakuCfgIndex}.");
            return Entity.Null;
        }

        Entity e_danmaku = _entityManager.CreateEntity();

        _entityManager.AddComponent(e_danmaku, new CDanmaku(danmakuCfgIndex));
        _entityManager.AddComponent(e_danmaku, new CPosition(emitPosX, emitPosY));
        _entityManager.AddComponent(e_danmaku, new CCollider{
            isActive = true,
            isDirty = false,
            type = danmakuCfg.colliderConfig.type,
            layer = danmakuCfg.colliderConfig.layer,
            mask = danmakuCfg.colliderConfig.mask,
            offsetX = danmakuCfg.colliderConfig.offset.x,
            offsetY = danmakuCfg.colliderConfig.offset.y,
            radius = danmakuCfg.colliderConfig.radius,
            height = danmakuCfg.colliderConfig.boxSize.y,
            width = danmakuCfg.colliderConfig.boxSize.x,
        });

        switch (emitterConfig.emitMode)
        {
            case EmitMode.Line:
                Vector2 dir = emitterConfig.lineDirection;
                _entityManager.AddComponent(e_danmaku, new CVelocity(emitterConfig.launchSpeed * dir.x, emitterConfig.launchSpeed * dir.y));
                break;
            case EmitMode.Arc:
                break;
        }

        //_entityManager.AddComponent(e_danmaku,new CPoolGet());

        return e_danmaku;
    }

    public Entity CreateEnemy(EnemyConfig enemyConfig, float posX, float posY)
    {
        Entity e_enemy = _entityManager.CreateEntity();
        //_entityManager.AddComponent(e_enemy, new CEnemy(enemyConfig.enemyType));
        _entityManager.AddComponent(e_enemy, new CPosition(posX, posY));
        _entityManager.AddComponent(e_enemy, new CVelocity(0, 0));
        _entityManager.AddComponent(e_enemy, new CCollider
        {
            isActive = true,
            isDirty = false,
            type = enemyConfig.colliderConfig.type,
            layer = enemyConfig.colliderConfig.layer,
            mask = enemyConfig.colliderConfig.mask,
            offsetX = enemyConfig.colliderConfig.offset.x,
            offsetY = enemyConfig.colliderConfig.offset.y,
            radius = enemyConfig.colliderConfig.radius,
            height = enemyConfig.ColliderSize.y,
            width = enemyConfig.ColliderSize.x,
        });
        //_entityManager.AddComponent(e_enemy,new CPoolGet());
        return e_enemy;
    }
}
