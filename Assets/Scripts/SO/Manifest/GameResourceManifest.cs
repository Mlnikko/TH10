using UnityEngine;

[CreateAssetMenu(fileName = "GameResourceManifest", menuName = "Configs/Manifest/GameResourceManifest")]
public class GameResourceManifest : ScriptableObject
{
    [Header("配置 - Configs")]
    public string[] characterConfigIds = new string[0];
    public string[] enemyConfigIds = new string[0];
    public string[] weaponConfigIds = new string[0];
    public string[] danmakuConfigIds = new string[0];
    public string[] danmakuEmitterConfigIds = new string[0];
    public string battleAreaConfigId;

    [Header("预制体 - Prefabs")]
    public string[] characterPrefabIds = new string[0];
    public string[] enemyPrefabIds = new string[0];
    public string[] danmakuPrefabIds = new string[0];
    public string[] danmakuEmitterPrefabIds = new string[0];
    public string[] effectPrefabIds = new string[0];

    [Header("贴图 - Textures")]
    public string[] characterImages = new string[0];

    [Header("图集 - Atlases")]
    public string[] atlases = new string[0];
}
