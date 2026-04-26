
using UnityEngine;

[CreateAssetMenu(fileName = "NewBattleAreaConfig", menuName = "Configs/Battle/BattleAreaConfig")]
public class BattleAreaConfig : GameConfig
{
    public BattleAreaData battleAreaData = BattleAreaData.Default;
    public PlayerSpawnData playerSpawnData;
}
