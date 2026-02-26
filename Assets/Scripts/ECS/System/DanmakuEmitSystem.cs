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
         
            if (!emitter.isEnabled) continue;

            var emitterConfig = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitter.cfgIndex);

            if(emitterConfig == null) continue;

            var position = positions[entity];

            switch (emitterConfig.emitMode)
            {
                case EmitMode.Line:
                    LineMode(position.x, position.y, emitterConfig);
                    break;
                case EmitMode.Arc:
                    ArcMode(position.x, position.y, emitterConfig);
                    break;
                case EmitMode.None:
                    break;
                default:
                    Logger.Error($"Unknown DanmakuEmitMode: {emitterConfig.emitMode}");
                    break;
            }
        }
    }

    /// <summary>
    /// 只发射第一种弹幕配置，用于单一弹幕发射或测试模式
    /// </summary>
    /// <param name="emitPosX"></param>
    /// <param name="emitPosY"></param>
    /// <param name="emitterConfig"></param>
    void EmitFirst(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig)
    {

        if (emitterConfig.danmakuCfgIndices.Length == 0)
        {
            Logger.Warn("No danmaku configurations available for emission.");
            return;
        }

        int danmakuCfgIndex = emitterConfig.danmakuCfgIndices[0];

       
        Entity e_danmaku = EntityFactory.CreateDanmaku(emitterConfig, danmakuCfgIndex, emitPosX, emitPosY);

        EntityManager.AddComponent(e_danmaku, new CPoolGet());

        //Logger.Info($"Emitted danmaku entity {e_danmaku.Index} at position ({emitPosX}, {emitPosY}) with config index {danmakuCfgIndex}.");
    }

    /// <summary>
    /// 按照配置的顺序依次发射弹幕，适用于多种弹幕组合的发射模式
    /// </summary>
    /// <param name="emitPosX"></param>
    /// <param name="emitPosY"></param>
    /// <param name="emitterConfig"></param>
    /// <param name="danmakuIndices"></param>
    void EmitSequential(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig, int[] danmakuIndices)
    {

    }

    void EmitRandom(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig, int[] danmakuIndices)
    {

    }

    void LineMode(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig)
    {
        int lineCount = emitterConfig.lineCount;
        if (lineCount > 0)
        {
            float spacing = emitterConfig.lineSpacing;
            float dirX = emitterConfig.lineDirection.x;
            float dirY = emitterConfig.lineDirection.y;

            for (int i = 0; i < lineCount; i++)
            {
                float offsetX = (i - (lineCount - 1) / 2f) * spacing * dirY;
                float offsetY = (i - (lineCount - 1) / 2f) * spacing * -dirX;
                switch(emitterConfig.danmakuSelectMode)
                {
                    case DanmakuSelectMode.First:
                        EmitFirst(emitPosX + offsetX, emitPosY + offsetY, emitterConfig);
                        break;
                    case DanmakuSelectMode.Sequential:
                        //EmitSequential(emitPosX + offsetX, emitPosY + offsetY, emitterConfig, danmakuIndices);
                        break;
                    case DanmakuSelectMode.Random:
                        //EmitRandom(emitPosX + offsetX, emitPosY + offsetY, emitterConfig, danmakuIndices);
                        break;
                    default:
                        Logger.Error($"Unknown DanmakuSelectMode: {emitterConfig.danmakuSelectMode}");
                        break;
                }
            }
        }
    }

    void ArcMode(float emitPosX, float emitPosY, DanmakuEmitterConfig emitterConfig)
    {
        int danmakuCount = emitterConfig.danmakuCfgIndices.Length;
        if (danmakuCount > 0)
        {
            float angleStep = emitterConfig.arcAngle / (danmakuCount - 1);
            float startAngle = emitterConfig.arcStartAngle;
            for (int i = 0; i < danmakuCount; i++)
            {
                float angle = startAngle + i * angleStep;
                float offsetX = Mathf.Cos(angle * Mathf.Deg2Rad) * emitterConfig.arcRadius;
                float offsetY = Mathf.Sin(angle * Mathf.Deg2Rad) * emitterConfig.arcRadius;
                switch(emitterConfig.danmakuSelectMode)
                {
                    case DanmakuSelectMode.First:
                        EmitFirst(emitPosX + offsetX, emitPosY + offsetY, emitterConfig);
                        break;
                    case DanmakuSelectMode.Sequential:
                        //EmitSequential(emitPosX + offsetX, emitPosY + offsetY, emitterConfig, danmakuIndices);
                        break;
                    case DanmakuSelectMode.Random:
                        //EmitRandom(emitPosX + offsetX, emitPosY + offsetY, emitterConfig, danmakuIndices);
                        break;
                    default:
                        Logger.Error($"Unknown DanmakuSelectMode: {emitterConfig.danmakuSelectMode}");
                        break;
                }
            }
        }
    }
}

