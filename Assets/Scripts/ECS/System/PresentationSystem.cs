using System;
using UnityEngine;

public class PresentationSystem : BaseSystem
{
    public override void OnLateUpdate(float deltaTime)
    {
        Span<int> indices = stackalloc int[1024]; // 假设每帧新生成的实体不超过 1024 个，不够会自动截断（需处理扩容逻辑或加大 buffer）
        int count = EntityManager.GetEntities<CPendingPresentation>(indices);

        if (count == 0) return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();
        var danmakus = EntityManager.GetComponentSpan<CDanmaku>();
        var enemies = EntityManager.GetComponentSpan<CEnemy>();
        var players = EntityManager.GetComponentSpan<CPlayer>();     

        for (int i = 0; i < count; i++)
        {
            int entityIndex = indices[i];
            Entity entity = EntityManager.GetEntityByIndex(entityIndex);

            if (!EntityManager.IsValid(entity)) continue;

            ref var pos = ref positions[entityIndex];
            Vector3 spawnPos = new Vector3(pos.x, pos.y, 0f);


            // --- 根据实体类型决定生成什么 ---
            GameObject go = null;
            IGameObjectUpdater updater = null;

            if (EntityManager.HasComponent<CDanmaku>(entity) && entityIndex < danmakus.Length)
            {
                // 【弹幕】
                ref var danmaku = ref danmakus[entityIndex];
                var config = GameResDB.Instance.GetConfig<DanmakuConfig>(danmaku.cfgIndex);
                if (config != null)
                {
                    // 2. 获取预制体索引
                    int prefabIndex = config.danmakuPrefabIndex;
                    go = GameObjectPoolManager.Instance.Get(prefabIndex);

                    if (go != null)
                    {
                        updater = new DanmakuUpdater(go);
                    }
                    else
                    {
                        Logger.Error($"Pool exhausted for Danmaku Config[{danmaku.cfgIndex}] (PrefabIndex:{prefabIndex})");
                    } 
                }
                else
                {
                    Logger.Error($"Danmaku Config not found for index: {danmaku.cfgIndex}");
                }
            }
            else
            {
                // 【未知类型或无配置】
                // 可以选择记录警告，或者直接跳过
                //Logger.Warn($"Entity {entity.Index} has PendingPresentation but no recognized type component.");
            }

            // === 4. 建立桥接 ===
            if (go != null && updater != null)
            {
                // 关键：调用 Bridge.Link，内部会添加 CGameObjectLink 组件
                World.GameObjectBridge.Link(entity, go, updater, EntityManager);
            }
            else
            {
                // 如果生成失败（如资源未加载），可以选择保留 Tag 下一帧重试，或者直接移除防止死循环
                // 对于 Addressable 异步加载，这里可能需要更复杂的逻辑（暂不展开）
                Logger.Error($"Failed to spawn presentation for Entity {entity.Index}");
            }

            // === 5. 清理标记 ===
            // 无论是否成功生成，都移除 Tag，避免下一帧重复处理
            // 如果生成失败且希望重试，则不要移除
            EntityManager.RemoveComponent<CPendingPresentation>(entity);
        }
    }
}
