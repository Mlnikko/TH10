using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyConfigViewer : MonoBehaviour
{
    public EnemyConfig EnemyConfig => enemyConfig;
    [SerializeField] EnemyConfig enemyConfig;

    [Header("敌人属性设置")]
    [SerializeField] EnemyType enemyType;
    [SerializeField] float maxHealth;

    [Header("碰撞设置")]
    [SerializeField] ColliderConfig colliderConfig;

    [Header("音频资源设置")]
    [SerializeField] AudioName dieAudioName;


    void Awake()
    {
        LoadEnemyConfig();
    }


    public void LoadEnemyConfig()
    {
        if (enemyConfig == null) return;

        enemyType = enemyConfig.enemyType;
        colliderConfig = enemyConfig.colliderConfig;

        Logger.Debug("已加载敌人配置：" + enemyConfig.name);
        
    }

    public void SaveEnemyConfig()
    {
        if (enemyConfig == null) return;
        enemyConfig.enemyType = enemyType;
        enemyConfig.colliderConfig = colliderConfig;
        Logger.Debug("已保存敌人配置：" + enemyConfig.name);
    }

    void OnDrawGizmosSelected()
    {
        if (enemyConfig == null) return;

        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, colliderConfig, Color.yellow, Color.green);
    }
}