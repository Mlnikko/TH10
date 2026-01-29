using UnityEngine;

[CreateAssetMenu(fileName = "GamePrefabManifest", menuName = "Configs/Manifest/GamePrefabManifest")]
public class GamePrefabManifest : GameConfig
{
    [Header("角色预制体ID列表")]
    public string[] characterPrefabIds = new string[0];

    [Header("敌人预制体ID列表")]
    public string[] enemyPrefabIds = new string[0];

    [Header("弹幕预制体ID列表")]
    public string[] danmakuPrefabIds = new string[0];

    [Header("通用预制体ID列表")]
    public string[] commonPrefabIds = new string[0];
}
