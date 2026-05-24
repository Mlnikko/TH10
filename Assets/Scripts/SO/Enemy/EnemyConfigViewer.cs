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

        Logger.Debug("已加载敌人配置：" + enemyConfig.name);
        
    }

    public void SaveEnemyConfig()
    {
        if (enemyConfig == null) return;
        enemyConfig.enemyType = enemyType;
        enemyConfig.maxHealth = maxHealth;
        enemyConfig.colliderConfig = colliderConfig;
        Logger.Debug("已保存敌人配置：" + enemyConfig.name);
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