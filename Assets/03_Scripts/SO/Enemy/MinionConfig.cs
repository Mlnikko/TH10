using UnityEngine;

[CreateAssetMenu(fileName = "NewMinionConfig", menuName = "Enemy/MinionConfig")]
public class MinionConfig : EnemyConfig
{
    public MinionConfig() : base() 
    {
        EnemyType = EnemyType.Minion;
    }
}
