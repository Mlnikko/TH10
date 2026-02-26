using UnityEngine;

public class EntityFactory
{
    readonly EntityManager _entityManager;
    public EntityFactory(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }
   
    public Entity CreatePlayer(PlayerBattleData playerBattleData, Vector3 pos, Vector3 rot)
    {
        Entity e_player = _entityManager.CreateEntity();

        _entityManager.AddComponent(e_player, new CPosition(pos.x, pos.y));
        _entityManager.AddComponent(e_player, new CVelocity(0, 0));
        _entityManager.AddComponent(e_player, new CPlayer(playerBattleData.playerIndex, (byte)playerBattleData.characterId, (byte)playerBattleData.weaponId));
        _entityManager.AddComponent(e_player, new CPlayerRunTime
        {
            isSlowMode = false,
        });

        _entityManager.AddComponent(e_player, new CPoolGet());

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
}
