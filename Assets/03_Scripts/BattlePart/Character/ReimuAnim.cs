using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReimuAnim : IPlayerAnim
{
    public string GetIdleAnimName()
    {
        return E_CharacterAnim.Reimu_Idle.ToString();
    }

    public string GetLeftMoveLoopAnimName()
    {
        return E_CharacterAnim.Reimu_LeftMove_Loop.ToString();
    }

    public string GetLeftMoveStartAnimName()
    {
        return E_CharacterAnim.Reimu_LeftMove_Start.ToString();
    }

    public string GetRightMoveLoopAnimName()
    {
        return E_CharacterAnim.Reimu_RightMove_Loop.ToString();
    }

    public string GetRightMoveStartAnimName()
    {
        return E_CharacterAnim.Reimu_RightMove_Start.ToString();
    }
}
