using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressableAutoConfig
{
    const string ASSETS_PREFIX = "Assets/";

    // 特殊路径前缀映射表：原始路径前缀 => 期望的 Addressable Key 前缀
    // 注意：必须以 "Assets/" 开头，且以 '/' 结尾
    static readonly Dictionary<string, string> PREFIX_MAPPINGS = new()
    {
        { "Assets/Art/", "Art/" },
        { "Assets/Audio/", "Audio/" },
        // 可继续添加，例如：
        // { "Assets/Resources/UI/", "UI/" },
    };

    [MenuItem("Tools/Addressables/Auto Configure All Keys")]
    public static void AutoConfigureAllKeys()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Logger.Error("Addressables 未初始化！", LogTag.Resource);
            return;
        }

        var group = settings.DefaultGroup;
        if (group == null)
        {
            Logger.Error("默认分组不存在！", LogTag.Resource);
            return;
        }

        int count = 0;

        // 配置所有 Texture2D
        ConfigureAssetsByType("t:Texture2D", ".png,.jpg", settings, group, ref count);

        // 配置所有 Sprite Atlas
        ConfigureAssetsByType("t:SpriteAtlas", ".spriteatlasv2", settings, group, ref count);

        // 配置所有 Prefab
        ConfigureAssetsByType("t:Prefab", ".prefab", settings, group, ref count);

        // 配置所有 ScriptableObject 配置
        ConfigureAssetsByType("t:ScriptableObject", ".asset", settings, group, ref count);

        // 可继续添加 VFX、Audio 等...

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Logger.Debug( $"自动配置了 {count} 个资源的 Addressable Key", LogTag.Resource);
    }

    static void ConfigureAssetsByType(
       string filter,
       string extensions,
       AddressableAssetSettings settings,
       AddressableAssetGroup group,
       ref int totalCount)
    {
        var guids = AssetDatabase.FindAssets(filter);
        var extList = new HashSet<string>(extensions.Split(','), StringComparer.OrdinalIgnoreCase);

        foreach (string guid in guids)
        {
            if (string.IsNullOrEmpty(guid)) continue;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.StartsWith(ASSETS_PREFIX, StringComparison.Ordinal)) continue;

            // 新增：跳过 Addressables 系统文件
            if (path.StartsWith("Assets/AddressableAssetsData/", StringComparison.Ordinal))
            {
                continue;
            }

            string ext = Path.GetExtension(path);
            if (!extList.Contains(ext)) continue;

            try
            {
                string expectedKey = GetAddressableKeyFromPath(path);

                var entry = settings.CreateOrMoveEntry(guid, group);
                if (entry == null)
                {
                    Logger.Warn( $"无法为资源创建 Addressable 条目: {path}（可能是系统保留资源）", LogTag.Resource);
                    continue;
                }

                if (entry.address != expectedKey)
                {
                    entry.address = expectedKey;
                    totalCount++;
                }
            }
            catch (Exception e)
            {
                Logger.Error( $"配置资源失败: {path}\n{e}", LogTag.Resource);
            }
        }
    }

    /// <summary>
    /// 根据资源路径生成 Addressable Key
    /// 规则：
    /// 1. 若路径匹配 PREFIX_MAPPINGS 中的某个前缀，则替换为映射值；
    /// 2. 否则，移除 "Assets/" 前缀；
    /// 3. 移除文件扩展名。
    /// </summary>
    static string GetAddressableKeyFromPath(string assetPath)
    {
        // 按长度降序排序，确保长前缀优先匹配（避免 Assets/A 匹配到 Assets/Art）
        var sortedMappings = PREFIX_MAPPINGS
            .OrderByDescending(kvp => kvp.Key.Length)
            .ToArray();

        foreach (var (originalPrefix, mappedPrefix) in sortedMappings)
        {
            if (assetPath.StartsWith(originalPrefix, StringComparison.Ordinal))
            {
                string relative = assetPath.Substring(originalPrefix.Length);
                return Path.ChangeExtension(mappedPrefix + relative, null);
            }
        }

        // 默认：移除 "Assets/"
        if (assetPath.StartsWith(ASSETS_PREFIX, StringComparison.Ordinal))
        {
            string relative = assetPath.Substring(ASSETS_PREFIX.Length);
            return Path.ChangeExtension(relative, null);
        }

        // 理论上不会走到这里
        return Path.ChangeExtension(assetPath, null);
    }
}