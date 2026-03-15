using System;

public class DanmakuSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CDanmaku>();

        var danmakus = EntityManager.GetComponentSpan<CDanmaku>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];

            ref var danmaku = ref danmakus[idx];
            ref var position = ref positions[idx];   
            ref var velocity = ref velocities[idx];

            UpdateDanmakuPosition(ref position, ref velocity);
            RecycleOutOfBoundsDanmaku(position, idx);
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
           EntityManager.AddComponent(entityIndex, new CPoolRecycleTag());
        }
    }
}
