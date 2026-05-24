#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 编辑器中按 ConfigId 查找配置资产。
/// </summary>
public static class ConfigViewerAssetLookup
{
    public static DanmakuConfig FindDanmakuConfig(string configId, string searchFolder = "Assets/Configs/Danmaku")
    {
        if (string.IsNullOrEmpty(configId))
            return null;

        string normalized = StringHelper.NormalizeResourceId(configId);
        string[] guids = AssetDatabase.FindAssets("t:DanmakuConfig", new[] { searchFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var cfg = AssetDatabase.LoadAssetAtPath<DanmakuConfig>(path);
            if (cfg != null && cfg.ConfigId == normalized)
                return cfg;
        }

        return null;
    }

    public static DanmakuEmitterConfig FindDanmakuEmitterConfig(
        string configId,
        string searchFolder = "Assets/Configs/DanmakuEmitter")
    {
        if (string.IsNullOrEmpty(configId))
            return null;

        string normalized = StringHelper.NormalizeResourceId(configId);
        string[] guids = AssetDatabase.FindAssets("t:DanmakuEmitterConfig", new[] { searchFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var cfg = AssetDatabase.LoadAssetAtPath<DanmakuEmitterConfig>(path);
            if (cfg != null && cfg.ConfigId == normalized)
                return cfg;
        }

        return null;
    }

    public static WeaponConfig FindWeaponConfig(string configId, string searchFolder = "Assets/Configs/Weapon")
    {
        if (string.IsNullOrEmpty(configId))
            return null;

        string normalized = StringHelper.NormalizeResourceId(configId);
        string[] guids = AssetDatabase.FindAssets("t:WeaponConfig", new[] { searchFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var cfg = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            if (cfg != null && cfg.ConfigId == normalized)
                return cfg;
        }

        return null;
    }
}
#endif
