using UnityEngine;

/// <summary>敌人预制体配置编辑；不参与运行时逻辑。</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => enemyConfig != null;

    public EnemyConfig EnemyConfig => enemyConfig;
    [SerializeField] EnemyConfig enemyConfig;

    [Header("敌人预制体")]
    [SerializeField] string enemyPrefabId;

    [Header("表现")]
    [SerializeField] Sprite displaySprite;
    [SerializeField] Color displayColor = Color.white;
    [SerializeField] float displayScale = 1f;
    [SerializeField] RuntimeAnimatorController animatorController;

    [Header("敌人属性设置")]
    [SerializeField] EnemyType enemyType;
    [SerializeField] int maxHealth;

    [Header("碰撞设置")]
    [SerializeField] ColliderConfig colliderConfig;

    [Header("死亡掉落")]
    [SerializeField] DeathDropEntry[] dropOnDeathEntries = System.Array.Empty<DeathDropEntry>();

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

        enemyPrefabId = enemyConfig.enemyPrefabId;
        displaySprite = enemyConfig.displaySprite;
        displayColor = enemyConfig.displayColor;
        displayScale = enemyConfig.displayScale;
        animatorController = enemyConfig.animatorController;
        enemyType = enemyConfig.enemyType;
        maxHealth = enemyConfig.maxHealth;
        colliderConfig = enemyConfig.colliderConfig;
        dropOnDeathEntries = enemyConfig.dropOnDeathEntries != null
            ? (DeathDropEntry[])enemyConfig.dropOnDeathEntries.Clone()
            : System.Array.Empty<DeathDropEntry>();
        deathEffectPrefabId = enemyConfig.deathEffectPrefabId;

        ApplyEditorPreview();
        Logger.Debug($"已加载敌人配置：{enemyConfig.name}");
    }

    protected override void ApplyEditorPreview() =>
        EnemyPresentation.Apply(displaySprite, displayColor, displayScale, animatorController, gameObject);

    public void SaveEnemyConfig()
    {
        if (enemyConfig == null) return;
        enemyConfig.enemyPrefabId = string.IsNullOrWhiteSpace(enemyPrefabId)
            ? string.Empty
            : enemyPrefabId.ToLowerInvariantTrimmed();
        enemyConfig.displaySprite = displaySprite;
        enemyConfig.displayColor = displayColor;
        enemyConfig.displayScale = displayScale;
        enemyConfig.animatorController = animatorController;
        enemyConfig.enemyType = enemyType;
        enemyConfig.maxHealth = maxHealth;
        enemyConfig.colliderConfig = colliderConfig;
        enemyConfig.dropOnDeathEntries = CloneDropEntries(dropOnDeathEntries);
        enemyConfig.deathEffectPrefabId = string.IsNullOrWhiteSpace(deathEffectPrefabId)
            ? string.Empty
            : deathEffectPrefabId.ToLowerInvariantTrimmed();
        Logger.Debug($"已保存敌人配置：{enemyConfig.name}");
    }

    static DeathDropEntry[] CloneDropEntries(DeathDropEntry[] entries)
    {
        if (entries == null || entries.Length == 0)
            return System.Array.Empty<DeathDropEntry>();

        var copy = new DeathDropEntry[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            copy[i] = entries[i];
            if (!string.IsNullOrWhiteSpace(copy[i].dropConfigId))
                copy[i].dropConfigId = copy[i].dropConfigId.ToLowerInvariantTrimmed();
            if (copy[i].count < 1)
                copy[i].count = 1;
        }

        return copy;
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
