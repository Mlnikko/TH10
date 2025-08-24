using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class EnemyPrefab : MonoBehaviour
{
    [SerializeField] EnemyConfig enemyConfig;

    [SerializeField] E_EnemyType enemyType;
    [SerializeField] E_EnemyName enemyName;

    public void LoadEnemyConfig()
    {
        if (enemyConfig == null) return;

        enemyType = enemyConfig.EnemyType;
        enemyName = enemyConfig.EnemyName;

        OnEnemyConfigLoad();
        GameLogger.Debug("已加载敌人配置" + enemyConfig.name);
    }

    protected abstract void OnEnemyConfigLoad();

    public void SaveEnemyConfig()
    {
        if (enemyConfig == null) return;

        enemyConfig.EnemyType = enemyType;
        enemyConfig.EnemyName = enemyName;

        OnEnemyConfigSave();
        GameLogger.Debug("已保存敌人配置" + enemyConfig.name);
    }

    protected abstract void OnEnemyConfigSave();
}
