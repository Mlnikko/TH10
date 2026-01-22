using System;

public class DanmakuEmitSystem : BaseSystem
{
    public override void OnLogicTick(uint tick)
    {
        Span<int> emitterIndices = TempBuffers.DanmakuEmitterIndices;
        int emitterCount = EntityManager.GetEntities<CDanmakuEmitter>(emitterIndices);

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var emitters = EntityManager.GetComponentSpan<CDanmakuEmitter>();
        var emitterRuntimes = EntityManager.GetComponentSpan<CDanmakuEmitterRunTime>();

        // 2. 遍历并处理发射逻辑
        for (int i = 0; i < emitterCount; i++)
        {
            int entity = emitterIndices[i];

            ref var emitter = ref emitters[entity];
            ref var emitterRuntime = ref emitterRuntimes[entity];
            var emitterConfig = ConfigManager.GetConfig<DanmakuEmitterConfig>(emitter.emitterConfigIndex.ToString());

            if (!emitterRuntime.isEnabled) return;

            var position = positions[entity];
            //CreateDanmakuEntity(position.x, position.y, emitterConfig);
        }
    }

    //void CreateDanmakuEntity(float emitterPosX, float emitterPosY, DanmakuEmitterConfig emitterCfg)
    //{
    //    Entity entity = EntityManager.CreateEntity();
    //    var danmakuConfig = ConfigManager.GetConfig<DanmakuConfig>(emitterCfg.DanmakuConfigId.ToString());
    //    if(danmakuConfig == null)
    //    {
    //        throw new Exception($"DanmakuConfig not found: {emitterCfg.DanmakuConfigId}");
    //    }
    //    // 设置组件
    //    EntityManager.AddComponent(entity, new CDanmaku
    //    {
    //         danmakuConfigIndex = emitterCfg.DanmakuConfigId,
    //    });

    //    EntityManager.AddComponent(entity, new CPosition
    //    {
    //        x = emitterPosX + emitterCfg.LaunchPosOffset.x,
    //        y = emitterPosY + emitterCfg.LaunchPosOffset.y
    //    });

    //    switch (emitterCfg.Type)
    //    {
    //        case EmitterType.Line:
    //            EntityManager.AddComponent(entity, new CVelocity
    //            {
    //                vx = emitterCfg.LineDirection.x * emitterCfg.LaunchSpeed,
    //                vy = emitterCfg.LineDirection.y * emitterCfg.LaunchSpeed
    //            });
    //            break;
    //        case EmitterType.Arc:
    //            break;
    //    }


    //    EntityManager.AddComponent(entity, new CCollider
    //    {
    //        type = danmakuConfig.ColliderType,
    //        layer = danmakuConfig.ColliderLayer,
    //        offsetX = danmakuConfig.ColliderOffset.x,
    //        offsetY = danmakuConfig.ColliderOffset.y,
    //        radius = danmakuConfig.Radius,
    //        width = danmakuConfig.Size.x,
    //        height = danmakuConfig.Size.y,
    //        active = true,
    //    });

    //    // 其他组件...
    //}
}

