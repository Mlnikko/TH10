using System;

public class DanmakuSystem : BaseSystem
{
    public override void OnLogicTick(uint tick)
    {
        Span<int> danmakuIndices = TempBuffers.DanmakuIndices;
        int danmakuCount = EntityManager.GetEntities<CDanmaku>(danmakuIndices);

        var danmakus = EntityManager.GetComponentSpan<CDanmaku>();
        var danmakuRuntimes = EntityManager.GetComponentSpan<CDanmakuRuntime>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var colliders = EntityManager.GetComponentSpan<CCollider>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();

        for (int i = 0; i < danmakuCount; i++)
        {
            int danmakuIndex = danmakuIndices[i];
            ref var danmaku = ref danmakus[danmakuIndex];
            ref var position = ref positions[danmakuIndex];
            ref var collider = ref colliders[danmakuIndex];       
            ref var velocity = ref velocities[danmakuIndex];
            ref var danmakuRuntime = ref danmakuRuntimes[danmakuIndex];

            var danmakuConfig = ConfigDB.GetDanmaku(danmaku.cfgIndex);

            switch (danmakuConfig.DanmakuType)
            {
                case DanmakuType.Normal:
                    position.x += danmakuRuntime.speed * velocity.vx;
                    position.y += danmakuRuntime.speed * velocity.vy;
                    break;
                case DanmakuType.Homing:
                    break;
            }
        }
    }
}
