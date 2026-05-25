using System;
using UnityEngine;
using System.Collections.Generic;

public enum EnemyType
{
    None = 0,
    Minion = 1,
    Elite = 2,
    Boss = 3
}


[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Configs/Enemy/EnemyConfig")]
public class EnemyConfig : GameConfig , IReferenceResolver
{
    public EnemyType enemyType;

    public string enemyPrefabId;
    [NonSerialized] public int enemyPrefabIndex;

    public string emitterConfigId;
    [NonSerialized] public int emitterConfigIndex;

    [Header("基础属性配置")]
    public int maxHealth;

    [Header("碰撞器配置")]
    public ColliderConfig colliderConfig;

    [Header("掉落物")]
    [Tooltip("被击杀时在死亡位置生成的 DropItemConfig 的 ConfigId（小写）；留空则不掉落")]
    public string[] dropOnDeathConfigIds = Array.Empty<string>();

    [NonSerialized]
    public int[] dropOnDeathCfgIndices = Array.Empty<int>();

    [Header("死亡表现")]
    [Tooltip("击杀时在死亡位置生成的纯粒子特效 prefab id（Effect 池，根节点挂 PooledEffectLifetime）；留空则不播放")]
    [PoolPrefabId(E_PoolCategory.Effect)]
    public string deathEffectPrefabId;

    [NonSerialized] public int deathEffectPrefabIndex = -1;

    public void ResolveReferences(GameResDB resDb)
    {
        // 1. 解析发射器预制体索引
        enemyPrefabIndex = resDb.GetPrefabIndex(enemyPrefabId);
        if (enemyPrefabIndex == -1)
        {
            Logger.Warn(
                $"[EnemyConfig] Prefab not found: '{enemyPrefabId}' " +
                $"(configId: {ConfigId})",
                LogTag.Resource
            );
        }

        // 2. 解析发射器配置索引
        emitterConfigIndex = resDb.GetConfigIndex(emitterConfigId);
        if (emitterConfigIndex == -1)
        {
            Logger.Warn(
                $"[EnemyConfig] Emitter config not found: '{emitterConfigId}' " +
                $"(configId: {ConfigId})",
                LogTag.Resource
            );
        }

        if (dropOnDeathConfigIds != null && dropOnDeathConfigIds.Length > 0)
        {
            var indices = new List<int>(dropOnDeathConfigIds.Length);
            for (int i = 0; i < dropOnDeathConfigIds.Length; i++)
            {
                string id = dropOnDeathConfigIds[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                int idx = resDb.GetConfigIndex(id.ToLowerInvariantTrimmed());
                if (idx >= 0)
                    indices.Add(idx);
                else
                    Logger.Warn($"[EnemyConfig] DropItemConfig not found: '{id}' (enemy {ConfigId})", LogTag.Resource);
            }
            dropOnDeathCfgIndices = indices.ToArray();
        }
        else
            dropOnDeathCfgIndices = Array.Empty<int>();

        deathEffectPrefabIndex = -1;
        if (!string.IsNullOrWhiteSpace(deathEffectPrefabId))
        {
            string effectId = deathEffectPrefabId.ToLowerInvariantTrimmed();
            deathEffectPrefabIndex = resDb.GetPrefabIndex(effectId);
            if (deathEffectPrefabIndex < 0)
            {
                Logger.Warn(
                    $"[EnemyConfig] Death effect prefab not found: '{effectId}' (enemy {ConfigId})",
                    LogTag.Resource);
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        enemyPrefabId = enemyPrefabId.ToLowerInvariantTrimmed();
        emitterConfigId = emitterConfigId.ToLowerInvariantTrimmed();
        deathEffectPrefabId = deathEffectPrefabId.ToLowerInvariantTrimmed();
        if (dropOnDeathConfigIds != null)
        {
            for (int i = 0; i < dropOnDeathConfigIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(dropOnDeathConfigIds[i]))
                    dropOnDeathConfigIds[i] = dropOnDeathConfigIds[i].ToLowerInvariantTrimmed();
            }
        }
    }
#endif
}
