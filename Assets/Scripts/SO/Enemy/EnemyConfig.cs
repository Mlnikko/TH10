using UnityEngine;

public enum EnemyType
{
    None = 0,
    Minion = 1,
    Elite = 2,
    Boss = 3
}


public class EnemyConfig : GameConfig
{
    public EnemyType enemyType;

    public ColliderConfig colliderConfig;
    public Vector2 ColliderSize;

    public float MaxHealth;

    public EnemyConfig() 
    { 
        enemyType = EnemyType.None;
        ColliderSize = new Vector2(1, 1);
        MaxHealth = 10f;
    }
}
