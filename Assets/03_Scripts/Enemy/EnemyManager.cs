using UnityEngine;

public class EnemyManager : SingletonMono<EnemyManager>
{
    ObjectPool<Enemy> enemyPool;
    protected override void OnSingletonInit()
    {
        
    }

    public void SpawnEnemy()
    {

    }

    public void RemoveEnemy(Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
    }
}
