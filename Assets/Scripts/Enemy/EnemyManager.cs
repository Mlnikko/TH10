using UnityEngine;

public class EnemyManager : SingletonMono<EnemyManager>
{
    protected override void OnSingletonInit()
    {
        
    }

    public void SpawnEnemy()
    {

    }

    public void RemoveEnemy(EnemyConfigViewer enemy)
    {
        enemy.gameObject.SetActive(false);
    }
}
