using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarisaAnim : IPlayerAnim
{
    public string GetIdleAnimName()
    {
        return E_CharacterAnim.Marisa_Idle.ToString();
    }

    public string GetLeftMoveLoopAnimName()
    {
        return E_CharacterAnim.Marisa_LeftMove_Loop.ToString();
    }

    public string GetLeftMoveStartAnimName()
    {
        return E_CharacterAnim.Marisa_LeftMove_Start.ToString();
    }

    public string GetRightMoveLoopAnimName()
    {
        return E_CharacterAnim.Marisa_RightMove_Loop.ToString();
    }

    public string GetRightMoveStartAnimName()
    {
        return E_CharacterAnim.Marisa_RightMove_Start.ToString();
    }
}
