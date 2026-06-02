using UnityEngine;

/// <summary>
/// 敌人运动：统一为 <see cref="PathRouteMovementData"/> 烘焙路径 + <see cref="CEnemyPathMovement"/>。
/// </summary>
public static class EnemyMovementBaking
{
    static int s_defaultDescentBakeIndex = -1;

    public static void ResetCachedRoutes() => s_defaultDescentBakeIndex = -1;

    public static CEnemyPathMovement CreateSimpleDescent(
        uint spawnFrame,
        float originX,
        float originY,
        float descentSpeedPerSecond = 3.6f)
    {
        int bakeIndex = GetOrCreateDefaultDescentBakeIndex(descentSpeedPerSecond);
        return new CEnemyPathMovement
        {
            spawnFrame = spawnFrame,
            originX = originX,
            originY = originY,
            routeBakeIndex = bakeIndex
        };
    }

    static int GetOrCreateDefaultDescentBakeIndex(float descentSpeedPerSecond)
    {
        if (s_defaultDescentBakeIndex >= 0)
            return s_defaultDescentBakeIndex;

        uint fps = GameManager.logicFPS > 0 ? (uint)GameManager.logicFPS : 60;
        var route = PathRouteMovementData.CreateLinearDown(48f, descentSpeedPerSecond);
        s_defaultDescentBakeIndex = EnemyPathBakeCache.Register(EnemyPathMovementBaking.BakeRoute(route, fps));
        return s_defaultDescentBakeIndex;
    }

    public static bool TryAttachMovementFromWave(
        EntityManager em,
        Entity entity,
        EnemyWaveConfig wave,
        int queueEntryIndex,
        uint spawnFrame,
        float originX,
        float originY)
    {
        int bakeIndex = wave.ResolvePathBakeIndex(queueEntryIndex);
        if (bakeIndex < 0)
        {
            uint fps = GameManager.logicFPS > 0 ? (uint)GameManager.logicFPS : 60;
            wave.BakePathRouteIfNeeded(fps);
            bakeIndex = wave.ResolvePathBakeIndex(queueEntryIndex);
        }

        if (bakeIndex >= 0)
        {
            em.AddComponent(entity, new CEnemyPathMovement
            {
                spawnFrame = spawnFrame,
                originX = originX,
                originY = originY,
                routeBakeIndex = bakeIndex
            });
            return true;
        }

        if (wave.useDefaultDescentIfNoMovement)
        {
            em.AddComponent(entity, CreateSimpleDescent(spawnFrame, originX, originY, wave.defaultDescentSpeed));
            return true;
        }

        return false;
    }

    public static bool TryAttachMovementFromBakeIndex(
        EntityManager em,
        Entity entity,
        int routeBakeIndex,
        uint spawnFrame,
        float originX,
        float originY)
    {
        if (routeBakeIndex < 0)
            return false;

        em.AddComponent(entity, new CEnemyPathMovement
        {
            spawnFrame = spawnFrame,
            originX = originX,
            originY = originY,
            routeBakeIndex = routeBakeIndex
        });
        return true;
    }
}
