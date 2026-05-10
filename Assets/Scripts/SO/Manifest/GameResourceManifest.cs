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
    /// <summary>掉落物配置（ConfigId，如 drop_point）。</summary>
    public string[] dropItemConfigIds = new string[0];
    public string[] poolConfigIds = new string[0];
    public string[] stageTimelineConfigIds = new string[0];
    public string battleAreaConfigId = "battlearea";

    [Header("预制体 - Prefabs")]
    public string[] characterPrefabIds = new string[0];
    public string[] enemyPrefabIds = new string[0];
    public string[] danmakuPrefabIds = new string[0];
    public string[] danmakuEmitterPrefabIds = new string[0];
    public string[] effectPrefabIds = new string[0];

    /// <summary>
    /// 掉落物专用预制体 id（小写）。<see cref="DropItemConfig.pickupPrefabId"/> 应使用此处或其它预制体数组中已登记的 id；
    /// 若与弹幕等共用同一预制体，只需在其中一侧数组登记即可（加载时会去重）。
    /// </summary>
    public string[] dropItemPrefabIds = new string[0];

    [Header("贴图 - Textures")]
    public string[] characterImages = new string[0];

    [Header("图集 - Atlases")]
    public string[] atlases = new string[0];
}
