using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public static class AddressableAutoConfig
{
    const string ASSETS_PREFIX = "Assets/";

    static readonly HashSet<string> EXCLUDED_PATH_PREFIXES = new(StringComparer.OrdinalIgnoreCase)
    {
        "Assets/Plugins/",
        "Assets/Editor/",
        "Assets/StreamingAssets/",
        "Assets/AddressableAssetsData/",
        "Assets/TextMesh Pro/",
        "Assets/Demigiant/",
        "Assets/LeanTween/"
        // 按需补充
    };

    // 核心：资源类型 -> 前缀映射
    static readonly Dictionary<string, (string[] extensions, string prefix)> s_typeConfigs = new()
    {
        { "t:Prefab", (new[] { ".prefab" }, "prefab") },
        { "t:ScriptableObject", (new[] { ".asset" }, "cfg") },
        { "t:AudioClip", (new[] { ".wav", ".mp3", ".ogg", ".aif", ".aiff" }, "se") },
        { "t:Texture2D", (new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp" }, "tex") },
        { "t:SpriteAtlas", (new[] { ".spriteatlas", ".spriteatlasv2" }, "atlas") },
        { "t:Shader", (new[] { ".shader", ".compute" }, "shader") },
        // 可继续扩展：VFX, Material, etc.
    };

    [MenuItem("Tools/Addressables/Auto Configure All Keys (With Type Prefix)")]
    public static void AutoConfigureAllKeys()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Logger.Error("Addressables 未初始化！");
            return;
        }

        var group = settings.DefaultGroup;
        if (group == null)
        {
            Logger.Error("默认分组不存在！");
            return;
        }

        int count = 0;
        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 忽略大小写防冲突

        foreach (var kvp in s_typeConfigs)
        {
            string filter = kvp.Key;
            var (extensions, prefix) = kvp.Value;
            ConfigureAssetsByType(filter, extensions, prefix, settings, group, ref count, allKeys);
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Logger.Info($"已更新 {count} 个资源的 Addressable Key（带类型前缀）");
    }

    static void ConfigureAssetsByType(string filter, string[] extensions, string prefix, AddressableAssetSettings settings, AddressableAssetGroup group, ref int totalCount, HashSet<string> allKeys)
    {
        var guids = AssetDatabase.FindAssets(filter);
        var extSet = new HashSet<string>(extensions.Select(e => e.ToLowerInvariant()));

        foreach (string guid in guids)
        {
            if (string.IsNullOrEmpty(guid)) continue;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.StartsWith(ASSETS_PREFIX, StringComparison.Ordinal))
                continue;

            if (EXCLUDED_PATH_PREFIXES.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                continue;

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (!extSet.Contains(ext))
                continue;

            try
            {
                string baseName = Path.GetFileNameWithoutExtension(path);
                string key = $"{prefix}_{baseName}".ToLowerInvariant();

                if (allKeys.Contains(key))
                {
                    Logger.Warn($"重复的 Addressable Key: '{key}'（路径: {path}）\n请重命名以避免冲突！");
                }
                else
                {
                    allKeys.Add(key);
                }

                var entry = settings.CreateOrMoveEntry(guid, group);
                if (entry == null)
                {
                    Logger.Warn($"无法为资源创建 Addressable 条目: {path}");
                    continue;
                }

                if (entry.address != key)
                {
                    entry.address = key;
                    totalCount++;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"配置资源失败: {path}\n{e}");
            }
        }
    }
}