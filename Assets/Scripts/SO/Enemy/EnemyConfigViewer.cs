using UnityEngine;

/// <summary>敌人预制体配置编辑；不参与运行时逻辑。</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => enemyConfig != null;

    public EnemyConfig EnemyConfig => enemyConfig;
    [SerializeField] EnemyConfig enemyConfig;

    [Header("敌人属性设置")]
    [SerializeField] EnemyType enemyType;
    [SerializeField] int maxHealth;

    [Header("碰撞设置")]
    [SerializeField] ColliderConfig colliderConfig;

    [Header("死亡掉落")]
    [Tooltip("DropItemConfig 的 ConfigId；留空则不掉落")]
    [SerializeField] string[] dropOnDeathConfigIds = System.Array.Empty<string>();

    [Header("死亡表现")]
    [Tooltip("Effect 池纯粒子 prefab（根节点 PooledEffectLifetime）；留空则不播放")]
    [PoolPrefabId(E_PoolCategory.Effect)]
    [SerializeField] string deathEffectPrefabId;

    [Header("音频资源设置")]
    [SerializeField] AudioName dieAudioName;


    public void LoadEnemyConfig() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (enemyConfig == null)
            return;

        enemyType = enemyConfig.enemyType;
        maxHealth = enemyConfig.maxHealth;
        colliderConfig = enemyConfig.colliderConfig;
        dropOnDeathConfigIds = enemyConfig.dropOnDeathConfigIds != null
            ? (string[])enemyConfig.dropOnDeathConfigIds.Clone()
            : System.Array.Empty<string>();
        deathEffectPrefabId = enemyConfig.deathEffectPrefabId;

        Logger.Debug("已加载敌人配置：" + enemyConfig.name);
        
    }

    public void SaveEnemyConfig()
    {
        if (enemyConfig == null) return;
        enemyConfig.enemyType = enemyType;
        enemyConfig.maxHealth = maxHealth;
        enemyConfig.colliderConfig = colliderConfig;
        enemyConfig.dropOnDeathConfigIds = NormalizeDropIds(dropOnDeathConfigIds);
        enemyConfig.deathEffectPrefabId = string.IsNullOrWhiteSpace(deathEffectPrefabId)
            ? string.Empty
            : deathEffectPrefabId.ToLowerInvariantTrimmed();
        Logger.Debug("已保存敌人配置：" + enemyConfig.name);
    }

    static string[] NormalizeDropIds(string[] ids)
    {
        if (ids == null || ids.Length == 0)
            return System.Array.Empty<string>();

        var list = new System.Collections.Generic.List<string>(ids.Length);
        for (int i = 0; i < ids.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(ids[i]))
                continue;
            list.Add(ids[i].ToLowerInvariantTrimmed());
        }

        return list.ToArray();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (enemyConfig == null)
            return;

        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, colliderConfig, Color.yellow, Color.green);
    }
#endif
}