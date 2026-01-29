using System;

public class DanmakuSystem : BaseSystem
{
    public override void OnLogicTick(uint tick)
    {
        Span<int> danmakuIndices = TempBuffers.DanmakuIndices;
        int danmakuCount = EntityManager.GetEntities<CDanmaku>(danmakuIndices);

        var danmakus = EntityManager.GetComponentSpan<CDanmaku>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();

        for (int i = 0; i < danmakuCount; i++)
        {
            int danmakuIndex = danmakuIndices[i];
            ref var danmaku = ref danmakus[danmakuIndex];
            ref var position = ref positions[danmakuIndex];   
            ref var velocity = ref velocities[danmakuIndex];

            UpdateDanmakuPosition(ref position, ref velocity);
        }
    }
    
    void UpdateDanmakuPosition(ref CPosition position, ref CVelocity velocity)
    {
        position.x += velocity.vx;
        position.y += velocity.vy;
    }
}
