#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器中按 ConfigId / PrefabId 查找资产（路径约定见各方法默认 folder）。
/// </summary>
public static class ConfigViewerAssetLookup
{
    public static T FindConfig<T>(string configId, string searchFolder = null)
        where T : GameConfig
    {
        if (string.IsNullOrEmpty(configId))
            return null;

        searchFolder ??= GetDefaultConfigFolder<T>();
        if (string.IsNullOrEmpty(searchFolder))
            return null;

        string normalized = StringHelper.NormalizeResourceId(configId);
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var cfg = AssetDatabase.LoadAssetAtPath<T>(path);
            if (cfg != null && cfg.ConfigId == normalized)
                return cfg;
        }

        return null;
    }

    public static GameObject FindPrefab(string prefabId, string searchFolder)
    {
        if (string.IsNullOrEmpty(prefabId) || string.IsNullOrEmpty(searchFolder))
            return null;

        string normalized = StringHelper.NormalizeResourceId(prefabId);
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { searchFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path))
                continue;

            string id = StringHelper.NormalizeResourceId(Path.GetFileNameWithoutExtension(path));
            if (id == normalized)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return null;
    }

    public static DanmakuConfig FindDanmakuConfig(string configId) =>
        FindConfig<DanmakuConfig>(configId, "Assets/Configs/Danmaku");

    public static DanmakuEmitterConfig FindDanmakuEmitterConfig(string configId) =>
        FindConfig<DanmakuEmitterConfig>(configId, "Assets/Configs/DanmakuEmitter");

    public static GameObject FindDanmakuEmitterPrefab(string prefabId) =>
        FindPrefab(prefabId, "Assets/Prefabs/DanmakuEmitter");

    public static WeaponConfig FindWeaponConfig(string configId) =>
        FindConfig<WeaponConfig>(configId, "Assets/Configs/Weapon");

    static string GetDefaultConfigFolder<T>() where T : GameConfig
    {
        if (typeof(T) == typeof(DanmakuConfig))
            return "Assets/Configs/Danmaku";
        if (typeof(T) == typeof(DanmakuEmitterConfig))
            return "Assets/Configs/DanmakuEmitter";
        if (typeof(T) == typeof(WeaponConfig))
            return "Assets/Configs/Weapon";
        return null;
    }
}
#endif
