using System;
using static Unity.Burst.Intrinsics.X86.Avx;

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

            ref var emitterComp = ref emitters[entity];
            ref var emitterRuntime = ref emitterRuntimes[entity];

            var emitterConfig = ConfigDB.GetEmitter(emitterComp.cfgIndex);
            var danmakuIndices = ConfigDB.GetEmitterDanmakuIndices(emitterComp.cfgIndex);

            if (!emitterRuntime.isEnabled) return;

            var position = positions[entity];
            
        }
    }
}

