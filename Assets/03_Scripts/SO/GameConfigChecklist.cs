using UnityEngine;

[CreateAssetMenu(fileName = "GameConfigChecklist", menuName = "Configs/GameConfigChecklist")]
public class GameConfigChecklist : GameConfig
{
    [Header("角色配置ID列表")]
    public string[] characterConfigIds = new string[0];

    [Header("武器配置ID列表")]
    public string[] weaponConfigIds = new string[0];

    [Header("弹幕配置ID列表")]
    public string[] danmakuConfigIds = new string[0];

    [Header("弹幕发射器配置ID列表")]
    public string[] emitterConfigIds = new string[0];

    //[Header("关卡配置ID列表")]
    //public string[] levelConfigIds = new string[0];

    [Header("战斗区域")]
    public string BattleAreaCfgId = "DefaultBattleArea";
}
