using System;
using UnityEngine;

public enum E_EnemyType
{
    None,
    Minion,
    Elite,
    Boss
}

public enum E_EnemyName
{
    None,
    ZUN
}


[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Custom/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public E_EnemyType EnemyType;
    public E_EnemyName EnemyName;
    public Vector2 ColliderSize;

    public EnemyConfig() 
    { 
        EnemyType = E_EnemyType.None;
        EnemyName = E_EnemyName.None;
        ColliderSize = new Vector2(1, 1);
    }
}
