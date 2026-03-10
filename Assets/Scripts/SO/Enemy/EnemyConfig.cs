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

    [Header("碰撞器配置")]
    public ColliderConfig colliderConfig;

    [Header("基础属性配置")]
    public float MaxHealth;

    public void ResolveReferences(GameResDB resDb)
    {
        // 1. 解析发射器预制体索引
        enemyPrefabIndex = resDb.GetPrefabIndex(enemyPrefabId);
        if (enemyPrefabIndex == -1)
        {
            Logger.Warn(
                $"[DanmakuEmitterConfig] Prefab not found: '{enemyPrefabId}' " +
                $"(configId: {configId})",
                LogTag.Resource
            );
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(enemyPrefabId))
            enemyPrefabId = enemyPrefabId.ToLowerInvariant().Trim();
    }
#endif
}
