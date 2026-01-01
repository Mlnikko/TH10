using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerAnim
{
    string GetLeftMoveStartAnimName();
    string GetRightMoveStartAnimName();
    string GetLeftMoveLoopAnimName();
    string GetRightMoveLoopAnimName();
    string GetIdleAnimName();
}
