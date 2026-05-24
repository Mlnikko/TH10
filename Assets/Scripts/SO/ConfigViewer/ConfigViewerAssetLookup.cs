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
}
#endif
