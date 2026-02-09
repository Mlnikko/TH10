using System;
using UnityEngine;

public class DanmakuEmitSystem : BaseSystem
{
    public override void OnLogicTick(uint tick)
    {
        Span<int> emitterIndices = TempBuffers.DanmakuEmitterIndices;
        int emitterCount = EntityManager.GetEntities<CDanmakuEmitter>(emitterIndices);

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var emitters = EntityManager.GetComponentSpan<CDanmakuEmitter>();

        // 2. 遍历并处理发射逻辑
        for (int i = 0; i < emitterCount; i++)
        {
            int entity = emitterIndices[i];

            ref var emitter = ref emitters[entity];

            var emitterConfig = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitter.cfgIndex);

            if (!emitter.isEnabled) return;

            var position = positions[entity];

            switch (emitterConfig.danmakuSelectMode)
            {
                case DanmakuSelectMode.First:
                    EmitFirst(position.x, position.y, emitterConfig);
                    break;
                case DanmakuSelectMode.Sequential:
                    //EmitSequential(position.x, position.y, emitterConfig, danmakuIndices);
                    break;
                case DanmakuSelectMode.Random:
                    //EmitRandom(position.x, position.y, emitterConfig, danmakuIndices);
                    break;
                default:
                    Logger.Error($"Unknown DanmakuSelectMode: {emitterConfig.danmakuSelectMode}");
                    break;
            }
        }
    }

    void EmitFirst(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig)
    {

        if (emitterConfig.danmakuCfgIndices.Length == 0)
        {
            Logger.Warn("No danmaku configurations available for emission.");
            return;
        }

        int danmakuIndex = emitterConfig.danmakuCfgIndices[0];

        var danmakuCfg = GameResDB.Instance.GetConfig<DanmakuConfig>(danmakuIndex);

        if(danmakuCfg == null)
        {
            Logger.Error($"Danmaku configuration not found for index {danmakuIndex}.");
            return;
        }

        var danmakuEntity = EntityManager.CreateEntity();

        EntityManager.AddComponent(danmakuEntity, new CDanmaku { cfgIndex = danmakuIndex });
        EntityManager.AddComponent(danmakuEntity, new CPosition(emitPosX, emitPosY));

        switch(emitterConfig.emitMode)
        {
            case EmitMode.Line:
                Vector2 dir = emitterConfig.LineDirection;
                EntityManager.AddComponent(danmakuEntity, new CVelocity(emitterConfig.launchSpeed * dir.x, emitterConfig.launchSpeed * dir.y));
                break;
            case EmitMode.Arc:
                break;
        }

        //
        int prefabIndex = danmakuCfg.danmakuPrefabIndex;
        EntityManager.AddComponent(danmakuEntity, new CGameObjectLink(prefabIndex));

        Logger.Info($"Emitted danmaku entity {danmakuEntity.Index} at position ({emitPosX}, {emitPosY}) with config index {danmakuIndex}.");
    }

    void EmitSequential(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig, int[] danmakuIndices)
    {

    }

    void EmitRandom(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig, int[] danmakuIndices)
    {

    }
}

