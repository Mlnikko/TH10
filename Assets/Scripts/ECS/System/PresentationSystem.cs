using System;
using UnityEngine;

public class PresentationSystem : BaseSystem
{
    // 临时缓冲区大小，根据同屏最大生成/销毁数量调整
    const int BUFFER_SIZE = 2048;

    public override void OnLateUpdate(float deltaTime)
    {
        // 1. 处理生成 (Spawn)
        ProcessSpawns();

        // 2. 处理回收 (Despawn)
        ProcessDespawns();

        // 3. 处理同步 (Sync)
        //ProcessSyncs();
    }

    #region Spawn Logic
    void ProcessSpawns()
    {
        Span<int> indices = stackalloc int[BUFFER_SIZE];
        int count = EntityManager.GetEntities<CPoolGet>(indices);

        if (count == 0) return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var danmakus = EntityManager.GetComponentSpan<CDanmaku>();
        // 其他组件...

        for (int i = 0; i < count; i++)
        {
            int entityIndex = indices[i];
            Entity entity = EntityManager.GetEntityByIndex(entityIndex);

            ref var pos = ref positions[entityIndex];
            Vector3 spawnPos = new Vector3(pos.x, pos.y, 0f);

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
            // else if (EntityManager.HasComponent<CEnemy>(entity)) { ... }
            // else if (EntityManager.HasComponent<CPlayer>(entity)) { ... }

            if (go != null && updater != null)
            {
                // 建立桥接：添加 CGameObjectLink，注册 Updater
                World.GameObjectBridge.Link(entity, go, updater, EntityManager);
            }
            else
            {
                Logger.Error($"Failed to spawn presentation for Entity {entity.Index}");
                // 可选：如果失败，是否保留 CPoolGet 下一帧重试？通常直接移除防止死循环
            }

            // 清理生成标记
            EntityManager.RemoveComponent<CPoolGet>(entity);
        }
    }
    #endregion

    #region Despawn Logic
    private void ProcessDespawns()
    {
        Span<int> indices = stackalloc int[BUFFER_SIZE];
        // 查询所有标记为需要销毁的实体
        int count = EntityManager.GetEntities<CPoolRecycle>(indices);

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
                GameObjectPoolManager.Instance.Return(link.GameObject);
                World.GameObjectBridge.Unlink(entity, EntityManager);
            }
            else
            {
                Logger.Warn($"Entity {entity.Index} marked for despawn but has no CGameObjectLink.");
            }

            // 3. 清理标记组件
            EntityManager.RemoveComponent<CPoolRecycle>(entity);

            // 4. 销毁 ECS 实体本身
            EntityManager.DestroyEntity(entity);
        }
    }
    #endregion
}