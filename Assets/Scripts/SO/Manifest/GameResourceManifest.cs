using UnityEngine;

[CreateAssetMenu(fileName = "GameResourceManifest", menuName = "Configs/Manifest/GameResourceManifest")]
public class GameResourceManifest : ScriptableObject
{
    [Header("≈‰÷√ - Configs")]
    public string[] characterConfigIds = new string[0];
    public string[] enemyConfigIds = new string[0];
    public string[] weaponConfigIds = new string[0];
    public string[] danmakuConfigIds = new string[0];
    public string[] danmakuEmitterConfigIds = new string[0];
    public string battleAreaConfigId;

    [Header("‘§÷∆ÃÂ - Prefabs")]
    public string[] characterPrefabIds = new string[0];
    public string[] enemyPrefabIds = new string[0];
    public string[] danmakuPrefabIds = new string[0];
    public string[] danmakuEmitterPrefabIds = new string[0];
    public string[] effectPrefabIds = new string[0];

    [Header("Ã˘Õº - Textures")]
    public string[] characterImages = new string[0];

    [Header("ÕººØ - Atlases")]
    public string[] atlases = new string[0];
}
