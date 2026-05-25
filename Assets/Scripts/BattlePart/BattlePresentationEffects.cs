using UnityEngine;

/// <summary>
/// 战斗表现层<strong>纯粒子</strong>特效（<see cref="GameObjectPoolManager"/>，不参与 ECS / 锁步状态）。
/// </summary>
public static class BattlePresentationEffects
{
    public static void TrySpawnPooledEffect(int prefabIndex, float worldX, float worldY)
    {
        if (prefabIndex < 0)
            return;

        var pool = GameObjectPoolManager.Instance;
        if (pool == null)
            return;

        GameObject go = pool.Get(prefabIndex);
        if (go == null)
        {
            Logger.Warn(
                $"[BattlePresentationEffects] Pool Get failed for prefab index {prefabIndex}.",
                LogTag.Pool);
            return;
        }

        go.transform.SetPositionAndRotation(new Vector3(worldX, worldY, 0f), Quaternion.identity);
        go.SetActive(true);
        PooledEffectLifetime.ActivateAfterSpawn(go);
    }
}
