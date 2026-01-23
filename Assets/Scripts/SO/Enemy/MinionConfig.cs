using UnityEngine;

[CreateAssetMenu(fileName = "NewMinionConfig", menuName = "Configs/Enemy/MinionConfig")]
public class MinionConfig : EnemyConfig
{
    public MinionConfig() : base() 
    {
        EnemyType = EnemyType.Minion;
    }
}
