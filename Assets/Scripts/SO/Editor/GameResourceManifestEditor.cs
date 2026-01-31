using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameResourceManifest))]
public class GameResourceManifestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("自动识别填充资源ID"))
            FillResources();
    }

    void FillResources()
    {
        var manifest = (GameResourceManifest)target;

        // 清空现有数据
        foreach (var cat in manifest.resourceCategories)
            foreach (var grp in cat.resGroups)
                grp.resourceIds.Clear();

        Undo.RecordObject(manifest, "Fill Resources");

        // 按 category 分组收集需要处理的 group 及其物理路径
        var categoryToGroups = new Dictionary<E_ResourceCategory, List<(ResourceGroup group, string folder)>>();

        foreach (var cat in manifest.resourceCategories)
        {
            string basePath = GetBasePathForCategory(cat.resCategory);
            var groupList = new List<(ResourceGroup, string)>();

            foreach (var grp in cat.resGroups)
            {
                if (string.IsNullOrWhiteSpace(grp.groupName)) continue;

                string folder = $"Assets{basePath.TrimEnd('/')}/{grp.groupName}";
                if (!Directory.Exists(folder))
                {
                    Logger.Warn($"{cat.resCategory}分类下，{grp.groupName}组不存在，跳过: {folder}", LogTag.File, manifest);
                    continue;
                }

                groupList.Add((grp, folder));
            }

            if (groupList.Count > 0)
                categoryToGroups[cat.resCategory] = groupList;
        }

        // 按类型批量处理
        foreach (var kvp in categoryToGroups)
        {
            var groups = kvp.Value;

            // 一次性获取该类型下所有目标目录的资源 GUIDs
            string[] allFolders = groups.Select(g => g.folder).ToArray();
            string[] guids = GetAssetGUIDsByCategory(kvp.Key, allFolders);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                // 精确分配：找到 assetPath 所属的 group（按最长前缀匹配更安全）
                ResourceGroup targetGroup = null;
                int maxMatchLength = -1;

                foreach (var (grp, folder) in groups)
                {
                    // 确保路径以目录结尾（避免 Danmaku 匹配到 DanmakuBoss）
                    string normalizedFolder = folder.EndsWith("/") ? folder : folder + "/";
                    if (assetPath.StartsWith(normalizedFolder, System.StringComparison.OrdinalIgnoreCase))
                    {
                        int len = normalizedFolder.Length;
                        if (len > maxMatchLength)
                        {
                            maxMatchLength = len;
                            targetGroup = grp;
                        }
                    }
                }

                if (targetGroup != null)
                {
                    string baseName = Path.GetFileNameWithoutExtension(assetPath);
                    string id = $"{baseName}".ToLowerInvariant();

                    if (!targetGroup.resourceIds.Contains(id))
                    {
                        targetGroup.resourceIds.Add(id);
                    }
                }
            }
        }

        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();
        Logger.Debug("资源填充完成！（基于 Manifest 结构 + 批量扫描）");
    }

    static string GetBasePathForCategory(E_ResourceCategory category)
    {
        return category switch
        {
            E_ResourceCategory.Prefab => "/Prefabs/",
            E_ResourceCategory.Config => "/Configs/",
            E_ResourceCategory.Audio => "/Audio/",
            E_ResourceCategory.Texture => "/Art/Texture/",
            E_ResourceCategory.Atlas => "/Art/Atlas/",
            E_ResourceCategory.Shader => "/Shaders/",
            _ => "/Assets/"
        };
    }

    static string[] GetAssetGUIDsByCategory(E_ResourceCategory category, string[] searchInFolders)
    {
        return category switch
        {
            E_ResourceCategory.Prefab => AssetDatabase.FindAssets("t:Prefab", searchInFolders),
            E_ResourceCategory.Config => AssetDatabase.FindAssets("t:ScriptableObject", searchInFolders),
            E_ResourceCategory.Audio => AssetDatabase.FindAssets("t:AudioClip", searchInFolders),
            E_ResourceCategory.Texture => AssetDatabase.FindAssets("t:Texture2D", searchInFolders),
            E_ResourceCategory.Shader => AssetDatabase.FindAssets("t:Shader", searchInFolders),
            E_ResourceCategory.Atlas => AssetDatabase.FindAssets("t:SpriteAtlas", searchInFolders),
            _ => AssetDatabase.FindAssets("", searchInFolders)
        };
    }
}