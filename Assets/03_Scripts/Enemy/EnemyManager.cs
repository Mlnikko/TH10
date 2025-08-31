using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager
{
    public Enemy CreateEnemy(EnemyConfig enemyConfig)
    {
        return null;
    }

    public void RemoveEnemy(Enemy enemy)
    {
        CollisionSystem.RemoveCollider(enemy.Collider);
    }
}
