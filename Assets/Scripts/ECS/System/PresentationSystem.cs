using System;
using UnityEngine;

public class PresentationSystem : BaseSystem
{
    const int BUFFER_SIZE = 2048;

    public override void OnLateUpdate(float deltaTime)
    {
        // 1. 处理生成 (Spawn)
        ProcessSpawns();

        // 2. 处理回收 (Despawn)
        ProcessDespawns();
    }

    #region Spawn Logic
    void ProcessSpawns()
    {
        Span<int> indices = EntityManager.GetActiveIndices<CPoolGetTag>();

        if (indices.Length == 0) return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var drops = EntityManager.GetComponentSpan<CDropItem>();
        var danmakus = EntityManager.GetComponentSpan<CDanmaku>();
        var players = EntityManager.GetComponentSpan<CPlayer>();
        var enemies = EntityManager.GetComponentSpan<CEnemy>();

        // 其他组件...

        for (int i = 0; i < indices.Length; i++)
        {
            int entityIndex = indices[i];
            Entity entity = EntityManager.GetEntity(entityIndex);

            ref var pos = ref positions[entityIndex];
            Vector3 spawnPos = new(pos.x, pos.y, 0f);

            GameObject go = null;
            IGameObjectUpdater updater = null;
            string failureReason = null;

            // --- 根据实体类型决定生成什么 ---
            if (EntityManager.HasComponent<CDropItem>(entity) && entityIndex < drops.Length)
            {
                ref var drop = ref drops[entityIndex];
                var dropCfg = GameResDB.Instance.GetConfig<DropItemConfig>(drop.cfgIndex);

                if (dropCfg == null)
                    failureReason = $"DropItemConfig not found (cfgIndex={drop.cfgIndex})";
                else if (dropCfg.pickupPrefabIndex < 0)
                    failureReason = $"invalid pickupPrefabIndex ({dropCfg.pickupPrefabIndex}), prefabId='{dropCfg.pickupPrefabId}'";
                else
                {
                    go = GameObjectPoolManager.Instance.Get(dropCfg.pickupPrefabIndex);
                    if (go == null)
                        failureReason = $"pool Get returned null ({EntityPresentationDiagnostics.FormatPrefab(dropCfg.pickupPrefabIndex)})";
                    else
                    {
                        go.transform.position = spawnPos;
                        go.SetActive(true);
                        if (dropCfg.pickupSprite != null)
                        {
                            var sr = go.GetComponentInChildren<SpriteRenderer>();
                            if (sr != null)
                                sr.sprite = dropCfg.pickupSprite;
                        }
                        updater = new DropItemUpdater(go);
                    }
                }
            }
            else if (EntityManager.HasComponent<CDanmaku>(entity) && entityIndex < danmakus.Length)
            {
                ref var danmaku = ref danmakus[entityIndex];
                var config = GameResDB.Instance.GetConfig<DanmakuConfig>(danmaku.cfgIndex);

                if (config == null)
                    failureReason = $"DanmakuConfig not found (cfgIndex={danmaku.cfgIndex})";
                else
                {
                    int prefabIndex = config.danmakuPrefabIndex;
                    if (prefabIndex < 0)
                        failureReason = $"invalid danmakuPrefabIndex ({prefabIndex}), prefabId='{config.danmakuPrefabId}'";
                    else
                    {
                        go = GameObjectPoolManager.Instance.Get(prefabIndex);
                        if (go == null)
                            failureReason = $"pool Get returned null ({EntityPresentationDiagnostics.FormatPrefab(prefabIndex)})";
                        else
                        {
                            go.transform.position = spawnPos;
                            go.SetActive(true);
                            updater = new DanmakuUpdater(go);
                        }
                    }
                }
            }
            else if (EntityManager.HasComponent<CPlayer>(entity))
            {
                ref var player = ref players[entityIndex];
                var config = GameResDB.Instance.GetConfig<CharacterConfig>(player.characterCfgIndex);

                if (config == null)
                    failureReason = $"CharacterConfig not found (cfgIndex={player.characterCfgIndex})";
                else
                {
                    int prefabIndex = config.characterPrefabIndex;
                    if (prefabIndex < 0)
                        failureReason = $"invalid characterPrefabIndex ({prefabIndex}), prefabId='{config.characterPrefabId}'";
                    else
                    {
                        go = GameObjectPoolManager.Instance.Get(prefabIndex);
                        if (go == null)
                            failureReason = $"pool Get returned null ({EntityPresentationDiagnostics.FormatPrefab(prefabIndex)})";
                        else
                        {
                            go.transform.position = spawnPos;
                            go.SetActive(true);
                            var playerUpdater = new PlayerUpdater(go);
                            TryAttachWeaponPrefab(player.weaponCfgIndex, playerUpdater);
                            updater = playerUpdater;
                        }
                    }
                }
            }
            else if (EntityManager.HasComponent<CEnemy>(entity))
            {
                ref var enemy = ref enemies[entityIndex];
                var config = GameResDB.Instance.GetConfig<EnemyConfig>(enemy.enemyCfgIndex);

                if (config == null)
                    failureReason = $"EnemyConfig not found (cfgIndex={enemy.enemyCfgIndex})";
                else
                {
                    int prefabIndex = config.enemyPrefabIndex;
                    if (prefabIndex < 0)
                        failureReason = $"invalid enemyPrefabIndex ({prefabIndex}), prefabId='{config.enemyPrefabId}'";
                    else
                    {
                        go = GameObjectPoolManager.Instance.Get(prefabIndex);
                        if (go == null)
                            failureReason = $"pool Get returned null ({EntityPresentationDiagnostics.FormatPrefab(prefabIndex)})";
                        else
                        {
                            go.transform.position = spawnPos;
                            go.SetActive(true);
                            updater = new EnemyUpdater(go);
                        }
                    }
                }
            }
            else
            {
                failureReason = "entity has no presentation component (DropItem/Danmaku/Player/Enemy)";
            }

            if (go != null && updater != null)
            {
                // 建立桥接：添加 CGameObjectLink，注册 Updater
                World.GameObjectBridge.Link(entity, go, updater, EntityManager);
            }
            else
            {
                if (failureReason == null)
                    failureReason = go != null ? "updater is null" : "spawn preconditions not met";
                Logger.Error(
                    EntityPresentationDiagnostics.FormatSpawnFailure(EntityManager, entity, failureReason),
                    LogTag.Pool,
                    go);
            }

            EntityManager.RemoveComponent<CPoolGetTag>(entity);
        }
    }
    static void TryAttachWeaponPrefab(byte weaponCfgIndex, PlayerUpdater playerUpdater)
    {
        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(weaponCfgIndex);
        if (weaponConfig == null || weaponConfig.weaponPrefabIndex < 0)
            return;

        GameObject weaponGo = GameObjectPoolManager.Instance.Get(weaponConfig.weaponPrefabIndex);
        if (weaponGo == null)
        {
            Logger.Warn(
                $"Weapon prefab pool Get failed (cfgIndex={weaponCfgIndex}, prefabIndex={weaponConfig.weaponPrefabIndex}).",
                LogTag.Pool);
            return;
        }

        playerUpdater.AttachWeapon(weaponGo);
    }

    #endregion

    #region Despawn Logic
    private void ProcessDespawns()
    {
        Span<int> indices = EntityManager.GetActiveIndices<CPoolRecycleTag>();
        // 查询所有标记为需要销毁的实体
        int count = indices.Length;

        if (count == 0) return;

        var linkComponents = EntityManager.GetComponentSpan<CGameObjectLink>();

        for (int i = 0; i < count; i++)
        {
            int entityIndex = indices[i];
            Entity entity = EntityManager.GetEntity(entityIndex);

            // 1. 获取关联的 GameObjectLink
            bool hasLink = EntityManager.HasComponent<CGameObjectLink>(entity);

            if (hasLink && entityIndex < linkComponents.Length)
            {
                ref var link = ref linkComponents[entityIndex];
                //GameObjectPoolManager.Instance.Return(link.GameObject);
                World.GameObjectBridge.Unlink(entity, EntityManager);
            }
            else
            {
                Logger.Warn(
                    $"{EntityPresentationDiagnostics.FormatEntity(entity)} marked for despawn but has no CGameObjectLink. "
                    + EntityPresentationDiagnostics.DescribePresentationContext(EntityManager, entityIndex),
                    LogTag.Pool);
            }

            // 3. 清理标记组件
            EntityManager.RemoveComponent<CPoolRecycleTag>(entity);

            // 4. 销毁 ECS 实体本身
            EntityManager.DestroyEntity(entity);
        }
    }
    #endregion
}