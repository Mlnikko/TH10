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
            int entityIndex = danmakuIndices[i];

            ref var danmaku = ref danmakus[entityIndex];
            ref var position = ref positions[entityIndex];   
            ref var velocity = ref velocities[entityIndex];

            UpdateDanmakuPosition(ref position, ref velocity);
            RecycleOutOfBoundsDanmaku(position, entityIndex);
        }
    }
    
    void UpdateDanmakuPosition(ref CPosition position, ref CVelocity velocity)
    {
        position.x += velocity.vx;
        position.y += velocity.vy;
    }

    // 弹幕超出边界后回收
    void RecycleOutOfBoundsDanmaku(CPosition position, int entityIndex)
    {
       if(!GlobalBattleData.AreaData.IsPointInRecycleArea(position.x, position.y))
       {
           EntityManager.AddComponent(entityIndex, new CPoolRecycle());
        }
    }
}
