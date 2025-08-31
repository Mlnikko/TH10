using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minion : Enemy
{
    MinionConfig minionConfig;
    void Start()
    {
        
    }


    protected override void OnEnemyConfigLoad()
    {
        minionConfig = (MinionConfig)enemyConfig;
    }

    protected override void OnEnemyConfigSave()
    {
        minionConfig = (MinionConfig)enemyConfig;
    }
}
