using System;
using UnityEngine;

public enum EnemyType
{
    None = 0,
    Minion = 1,
    Elite = 2,
    /// <summary>关底 Boss（<see cref="MainBossEncounterConfig"/>）。</summary>
    Boss = 3,
    /// <summary>中场 Boss（<see cref="MidBossEncounterConfig"/>）；与关底 Boss 分档，避免共用类型与弹幕池。</summary>
    MidBoss = 4,
}

public static class EnemyTypeExtensions
{
    public static bool IsMainBoss(this EnemyType type) => type == EnemyType.Boss;
    public static bool IsMidBoss(this EnemyType type) => type == EnemyType.MidBoss;
    public static bool IsBossTier(this EnemyType type) => type.IsMainBoss() || type.IsMidBoss();
}


[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Configs/Enemy/EnemyConfig")]
public class EnemyConfig : GameConfig , IReferenceResolver
{
    public EnemyType enemyType;

    /// <summary>池 archetype 固定为 <see cref="EnemyPrefabArchetypes.Unit"/>。</summary>
    [NonSerialized] public int enemyPrefabIndex;

    [Header("表现（出池时应用到 enemy_tpl_unit）")]
    public Sprite displaySprite;
    public RuntimeAnimatorController animatorController;

    public string emitterConfigId;
    [NonSerialized] public int emitterConfigIndex;

    [Header("基础属性配置")]
    public int maxHealth;

    [Header("碰撞器配置")]
    public ColliderConfig colliderConfig;

    [Header("掉落物")]
    [Tooltip("被击杀时生成的掉落物种类与数量")]
    public DeathDropEntry[] dropOnDeathEntries = Array.Empty<DeathDropEntry>();

    [NonSerialized] public BakedDeathDropEntry[] dropOnDeathBaked = Array.Empty<BakedDeathDropEntry>();

    [SerializeField, HideInInspector] string[] dropOnDeathConfigIds;

    [Header("死亡表现")]
    [Tooltip("击杀时在死亡位置生成的纯粒子特效 prefab id（Effect 池，根节点挂 PooledEffectLifetime）；留空则不播放")]
    [PoolPrefabId(E_PoolCategory.Effect)]
    public string deathEffectPrefabId;

    [NonSerialized] public int deathEffectPrefabIndex = -1;

    public void ResolveReferences(GameResDB resDb)
    {
        enemyPrefabIndex = resDb.GetPrefabIndex(EnemyPrefabArchetypes.Unit);
        if (enemyPrefabIndex == -1)
        {
            Logger.Warn(
                $"[EnemyConfig] Prefab not found: '{EnemyPrefabArchetypes.Unit}' " +
                $"(configId: {ConfigId})",
                LogTag.Resource
            );
        }

        emitterConfigIndex = resDb.GetConfigIndex(emitterConfigId);
        if (emitterConfigIndex == -1)
        {
            Logger.Warn(
                $"[EnemyConfig] Emitter config not found: '{emitterConfigId}' " +
                $"(configId: {ConfigId})",
                LogTag.Resource
            );
        }

        EnsureDropEntriesMigrated();
        dropOnDeathBaked = DeathDropBaking.BakeEntries(dropOnDeathEntries, resDb, $"EnemyConfig {ConfigId}");

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

    void EnsureDropEntriesMigrated()
    {
        if (dropOnDeathEntries != null && dropOnDeathEntries.Length > 0)
            return;
        if (dropOnDeathConfigIds == null || dropOnDeathConfigIds.Length == 0)
            return;

        dropOnDeathEntries = new DeathDropEntry[dropOnDeathConfigIds.Length];
        for (int i = 0; i < dropOnDeathConfigIds.Length; i++)
        {
            dropOnDeathEntries[i] = new DeathDropEntry
            {
                dropConfigId = dropOnDeathConfigIds[i],
                count = 1,
            };
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        emitterConfigId = emitterConfigId.ToLowerInvariantTrimmed();
        deathEffectPrefabId = deathEffectPrefabId.ToLowerInvariantTrimmed();
        EnsureDropEntriesMigrated();
        DeathDropBaking.NormalizeEntries(dropOnDeathEntries);
    }
#endif
}
