using System;
using UnityEngine;

public enum EnemyType
{
    None = 0,
    Minion = 1,
    Elite = 2,
    Boss = 3
}


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

    public void ResolveReferences(GameResDB resDb)
    {
        // 1. 解析发射器预制体索引
        enemyPrefabIndex = resDb.GetPrefabIndex(enemyPrefabId);
        if (enemyPrefabIndex == -1)
        {
            Logger.Warn(
                $"[EnemyConfig] Prefab not found: '{enemyPrefabId}' " +
                $"(configId: {configId})",
                LogTag.Resource
            );
        }

        // 2. 解析发射器配置索引
        emitterConfigIndex = resDb.GetConfigIndex(emitterConfigId);
        if (emitterConfigIndex == -1)
        {
            Logger.Warn(
                $"[EnemyConfig] Emitter config not found: '{emitterConfigId}' " +
                $"(configId: {configId})",
                LogTag.Resource
            );
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        enemyPrefabId = enemyPrefabId.ToLowerInvariantTrimmed();
        emitterConfigId = emitterConfigId.ToLowerInvariantTrimmed();
    }
#endif
}
