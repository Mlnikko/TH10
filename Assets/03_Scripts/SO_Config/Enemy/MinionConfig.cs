using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMinionConfig", menuName = "Enemy/MinionConfig")]
public class MinionConfig : EnemyConfig
{
    public MinionConfig() : base() 
    {
        EnemyType = E_EnemyType.Minion;
    }
}
