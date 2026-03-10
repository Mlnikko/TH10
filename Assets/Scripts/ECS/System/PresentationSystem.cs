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
        Span<int> indices = stackalloc int[BUFFER_SIZE];
        int count = EntityManager.GetEntities<CPoolGetTag>(indices);

        if (count == 0) return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var danmakus = EntityManager.GetComponentSpan<CDanmaku>();
        var players = EntityManager.GetComponentSpan<CPlayer>();
        // 其他组件...

        for (int i = 0; i < count; i++)
        {
            int entityIndex = indices[i];
            Entity entity = EntityManager.GetEntityByIndex(entityIndex);

            ref var pos = ref positions[entityIndex];
            Vector3 spawnPos = new(pos.x, pos.y, 0f);

            GameObject go = null;
            IGameObjectUpdater updater = null;

            // --- 根据实体类型决定生成什么 ---
            if (EntityManager.HasComponent<CDanmaku>(entity) && entityIndex < danmakus.Length)
            {
                ref var danmaku = ref danmakus[entityIndex];
                var config = GameResDB.Instance.GetConfig<DanmakuConfig>(danmaku.cfgIndex);

                if (config != null)
                {
                    int prefabIndex = config.danmakuPrefabIndex;
                    go = GameObjectPoolManager.Instance.Get(prefabIndex); // 假设这是你的池管理器

                    if (go != null)
                    {
                        go.transform.position = spawnPos;
                        go.SetActive(true);
                        updater = new DanmakuUpdater(go);
                    }
                }
            }
            else if(EntityManager.HasComponent<CPlayer>(entity))
            {
                ref var player = ref players[entityIndex];
                var config = GameResDB.Instance.GetConfig<CharacterConfig>(player.characterCfgIndex);

                if(config != null)
                {
                    int prefabIndex = config.characterPrefabIndex;
                    go = GameObjectPoolManager.Instance.Get(prefabIndex);
                    if (go != null)
                    {
                        go.transform.position = spawnPos;
                        go.SetActive(true);
                        updater = new PlayerUpdater(go);
                    }
                }
            }

            if (go != null && updater != null)
            {
                // 建立桥接：添加 CGameObjectLink，注册 Updater
                World.GameObjectBridge.Link(entity, go, updater, EntityManager);           
            }
            else
            {
                Logger.Error($"Failed to spawn presentation for Entity {entity.Index}");
                // 可选：如果失败，是否保留 CPoolGetTag 下一帧重试？通常直接移除防止死循环
            }

            EntityManager.RemoveComponent<CPoolGetTag>(entity);
        }
    }
    #endregion

    #region Despawn Logic
    private void ProcessDespawns()
    {
        Span<int> indices = stackalloc int[BUFFER_SIZE];
        // 查询所有标记为需要销毁的实体
        int count = EntityManager.GetEntities<CPoolRecycleTag>(indices);

        if (count == 0) return;

        var linkComponents = EntityManager.GetComponentSpan<CGameObjectLink>();

        for (int i = 0; i < count; i++)
        {
            int entityIndex = indices[i];
            Entity entity = EntityManager.GetEntityByIndex(entityIndex);

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
                Logger.Warn($"Entity {entity.Index} marked for despawn but has no CGameObjectLink.");
            }

            // 3. 清理标记组件
            EntityManager.RemoveComponent<CPoolRecycleTag>(entity);

            // 4. 销毁 ECS 实体本身
            EntityManager.DestroyEntity(entity);
        }
    }
    #endregion
}