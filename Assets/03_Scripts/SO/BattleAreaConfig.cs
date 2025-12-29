
using UnityEngine;

[CreateAssetMenu(fileName = "NewBattleAreaConfig", menuName = "Configs/BattleAreaConfig", order = 1)]
public class BattleAreaConfig : GameConfig
{
    public BattleAreaData battleAreaData = BattleAreaData.Default;
}
