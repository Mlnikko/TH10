using UnityEngine;

[CreateAssetMenu(fileName = "NewMinionConfig", menuName = "Configs/EnemyConfigViewer/MinionConfig")]
public class MinionConfig : EnemyConfig
{
    public MinionConfig() : base() 
    {
        enemyType = EnemyType.Minion;
    }
}
