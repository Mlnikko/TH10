using UnityEngine;

[CreateAssetMenu(fileName = "GameConfigManifest", menuName = "Configs/GameConfigManifest")]
public class GameConfigManifest : GameConfig
{
    [Header("角色配置ID列表")]
    public string[] characterConfigIds = new string[0];

    [Header("武器配置ID列表")]
    public string[] weaponConfigIds = new string[0];

    [Header("弹幕配置ID列表")]
    public string[] danmakuConfigIds = new string[0];

    [Header("弹幕发射器配置ID列表")]
    public string[] emitterConfigIds = new string[0];

    [Header("战斗区域")]
    public string BattleAreaCfgId = "DefaultBattleArea";
}
