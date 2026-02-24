using System;

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

        int danmakuCfgIndex = emitterConfig.danmakuCfgIndices[0];

        Entity e_danmaku = EntityFactory.CreateDanmaku(emitterConfig, danmakuCfgIndex, emitPosX, emitPosY);

        Logger.Info($"Emitted danmaku entity {e_danmaku.Index} at position ({emitPosX}, {emitPosY}) with config index {danmakuCfgIndex}.");
    }

    void EmitSequential(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig, int[] danmakuIndices)
    {

    }

    void EmitRandom(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig, int[] danmakuIndices)
    {

    }
}

