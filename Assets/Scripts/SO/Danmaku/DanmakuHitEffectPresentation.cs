using UnityEngine;

/// <summary>
/// 弹幕回收时在表现层播放命中特效（不参与锁步逻辑）。
/// </summary>
public static class DanmakuHitEffectPresentation
{
    public static void TrySpawnOnRecycle(EntityManager em, int entityIndex)
    {
        if (!em.HasComponent<CDanmaku>(entityIndex) || !em.HasComponent<CPosition>(entityIndex))
            return;

        ref readonly var danmaku = ref em.GetComponentSpan<CDanmaku>()[entityIndex];
        var cfg = GameResDB.Instance.GetConfig<DanmakuConfig>(danmaku.cfgIndex);
        ref readonly var pos = ref em.GetComponentSpan<CPosition>()[entityIndex];
        TrySpawnAtConfig(cfg, pos.x, pos.y);
    }

    public static void TrySpawnAtConfig(DanmakuConfig cfg, float worldX, float worldY)
    {
        if (cfg == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            TrySpawnEditorPreview(cfg.hitEffectPrefabId, worldX, worldY);
            return;
        }
#endif

        if (cfg.hitEffectPrefabIndex >= 0)
            BattlePresentationEffects.TrySpawnPooledEffect(cfg.hitEffectPrefabIndex, worldX, worldY);
    }

#if UNITY_EDITOR
    static void TrySpawnEditorPreview(string prefabId, float worldX, float worldY)
    {
        if (string.IsNullOrWhiteSpace(prefabId))
            return;

        GameObject prefab = ConfigViewerAssetLookup.FindPrefab(prefabId, "Assets/Prefabs/Effect");
        if (prefab == null)
            return;

        var instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return;

        instance.transform.SetPositionAndRotation(new Vector3(worldX, worldY, 0f), Quaternion.identity);
        instance.SetActive(true);
        PooledEffectLifetime.ActivateAfterSpawn(instance);
    }
#endif
}
