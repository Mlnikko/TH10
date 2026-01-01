using System;
using UnityEngine;

public enum E_CharacterAnim
{
    None = 0,

    // Reimu
    Reimu_LeftMove_Start,
    Reimu_LeftMove_Loop,
    Reimu_RightMove_Start,
    Reimu_RightMove_Loop,
    Reimu_Idle,

    // Marisa
    Marisa_LeftMove_Start,
    Marisa_LeftMove_Loop,
    Marisa_RightMove_Start,
    Marisa_RightMove_Loop,
    Marisa_Idle
}

public enum E_EnemyAnim
{
    None = 0,

}

[Serializable]
public class CharacterAnim
{
    public string displayName;
    public E_Character character;
    public E_CharacterAnim leftMoveStartAnim;
    public E_CharacterAnim rightMoveStartAnim;
    public E_CharacterAnim idleAnim;
}

[Serializable]
public class EnemyAnim
{
   public E_EnemyAnim enemyAnim;
}

[CreateAssetMenu(fileName = "NewAnimConfig", menuName = "Configs/AnimConfig")]
public class AnimConfig : GameConfig
{
    public CharacterAnim[] characterAnims;
    public EnemyAnim[] enemyAnims;
}
