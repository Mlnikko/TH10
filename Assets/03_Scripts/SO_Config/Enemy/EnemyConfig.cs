using UnityEngine;

public enum EnemyType
{
    None = 0,
    Minion = 1,
    Elite = 2,
    Boss = 3
}

[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Custom/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public EnemyType EnemyType;
    public Vector2 ColliderSize;

    public float MaxHealth;

    public EnemyConfig() 
    { 
        EnemyType = EnemyType.None;
        ColliderSize = new Vector2(1, 1);
        MaxHealth = 10f;
    }
}
