using System;
using UnityEngine;

public class DanmakuSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CDanmaku>();

        var danmakus = EntityManager.GetComponentSpan<CDanmaku>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();
        var colliders = EntityManager.GetComponentSpan<CCollider>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];

            ref var danmaku = ref danmakus[idx];
            ref var position = ref positions[idx];   
            ref var rotation = ref rotations[idx];
            ref var velocity = ref velocities[idx];
            ref var collider = ref colliders[idx];

            UpdateDanmakuPosition(ref position, ref velocity);
            UpdateDanmakuRotation(ref rotation, ref velocity);
            RecycleOutOfBoundsDanmaku(position, idx);
        }
    }
    
    void UpdateDanmakuPosition(ref CPosition position, ref CVelocity velocity)
    {
        position.x += velocity.vx;
        position.y += velocity.vy;
    }

    void UpdateDanmakuRotation(ref CRotation rotation, ref CVelocity velocity)
    {
        rotation.angle = MathF.Atan2(velocity.vy, velocity.vx) * Mathf.Rad2Deg;
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
