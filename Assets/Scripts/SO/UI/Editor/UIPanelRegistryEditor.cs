using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIPanelRegistry))]
public class UIPanelRegistryEditor : Editor
{
    SerializedProperty _scanFolderProp;
    SerializedProperty _entriesProp;
    bool _mergeKeepFlags = true;
    bool _keepEntriesNotInScan = true;

    void OnEnable()
    {
        _scanFolderProp = serializedObject.FindProperty(nameof(UIPanelRegistry.panelPrefabScanFolder));
        _entriesProp = serializedObject.FindProperty(nameof(UIPanelRegistry.entries));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_scanFolderProp);

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("扫描填充", EditorStyles.boldLabel);
            _mergeKeepFlags = EditorGUILayout.ToggleLeft("合并模式：保留已有条目的关闭策略 / 全屏层级开关", _mergeKeepFlags);
            using (new EditorGUI.DisabledScope(!_mergeKeepFlags))
            {
                _keepEntriesNotInScan = EditorGUILayout.ToggleLeft("合并时保留「扫描结果中未出现」的旧条目", _keepEntriesNotInScan);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("扫描预制体并填充注册表"))
                ScanAndFill();
            if (GUILayout.Button("选取文件夹…", GUILayout.Width(110f)))
                PickScanFolder();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "在指定目录下递归查找 .prefab：读取继承 UIPanel 的非抽象脚本类名作为「面板类型」；" +
                "资源 id 使用预制体文件名（不含扩展名）的小写形式，与 Tools/Addressables 自动键规则一致。",
                MessageType.Info);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(_entriesProp, true);

        serializedObject.ApplyModifiedProperties();
    }

    void PickScanFolder()
    {
        string start = _scanFolderProp.stringValue;
        if (string.IsNullOrEmpty(start) || !start.StartsWith("Assets/", StringComparison.Ordinal))
            start = "Assets";
        string abs = Path.Combine(Application.dataPath, "..", start).Replace('\\', '/');
        abs = Path.GetFullPath(abs);
        string chosen = EditorUtility.OpenFolderPanel("选择面板预制体根目录", abs, "");
        if (string.IsNullOrEmpty(chosen))
            return;
        chosen = chosen.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');
        if (!chosen.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Warn($"请选择工程 Assets 下的目录（当前：{chosen}）", LogTag.Resource);
            return;
        }

        string relative = "Assets" + chosen.Substring(dataPath.Length);
        _scanFolderProp.stringValue = relative.TrimEnd('/');
        serializedObject.ApplyModifiedProperties();
    }

    void ScanAndFill()
    {
        var registry = (UIPanelRegistry)target;
        string folder = registry.panelPrefabScanFolder?.Trim();
        if (string.IsNullOrEmpty(folder))
        {
            Logger.Warn("[UIPanelRegistry] 扫描目录为空。", LogTag.Resource);
            return;
        }

        folder = folder.Replace('\\', '/').TrimEnd('/');
        if (!folder.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Warn($"[UIPanelRegistry] 扫描目录须位于 Assets 下：{folder}", LogTag.Resource);
            return;
        }

        string absFolder = Path.Combine(Application.dataPath, "..", folder).Replace('\\', '/');
        absFolder = Path.GetFullPath(absFolder);
        if (!Directory.Exists(absFolder))
        {
            Logger.Warn($"[UIPanelRegistry] 目录不存在：{folder}", LogTag.Resource);
            return;
        }

        Undo.RecordObject(registry, "Scan UIPanel Prefabs Into Registry");

        var scanned = new List<(string typeName, string prefabId, string assetPath)>();
        var prefabIdOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            if (!TryResolveConcreteUIPanelType(prefab, out Type panelType))
            {
                Logger.Warn($"[UIPanelRegistry] 跳过（未找到 UIPanel 子类组件）：{path}", LogTag.Resource);
                continue;
            }

            string typeName = panelType.Name;
            string prefabId = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            if (prefabIdOwners.TryGetValue(prefabId, out string otherPath))
            {
                Logger.Warn($"[UIPanelRegistry] 重复的预制体 id「{prefabId}」：\n  {otherPath}\n  {path}", LogTag.Resource);
                continue;
            }

            prefabIdOwners[prefabId] = path;

            if (scanned.Any(s => s.typeName == typeName))
            {
                Logger.Warn($"[UIPanelRegistry] 面板类型「{typeName}」出现多次，保留首次：{path}", LogTag.Resource);
                continue;
            }

            scanned.Add((typeName, prefabId, path));
        }

        scanned.Sort((a, b) => string.CompareOrdinal(a.typeName, b.typeName));

        UIPanelRegistryEntry[] next;

        if (!_mergeKeepFlags)
        {
            next = scanned.Select(s => new UIPanelRegistryEntry
            {
                panelScriptTypeName = s.typeName,
                prefabResourceId = s.prefabId,
                destroyInstanceWhenClosed = true,
                exclusiveFullscreen = false
            }).ToArray();
        }
        else
        {
            var byType = new Dictionary<string, UIPanelRegistryEntry>(StringComparer.Ordinal);
            if (registry.entries != null)
            {
                foreach (var e in registry.entries)
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.panelScriptTypeName))
                        continue;
                    string key = e.panelScriptTypeName.Trim();
                    byType[key] = e;
                }
            }

            var list = new List<UIPanelRegistryEntry>();
            foreach (var s in scanned)
            {
                if (byType.TryGetValue(s.typeName, out var old))
                {
                    old.prefabResourceId = s.prefabId;
                    list.Add(old);
                    byType.Remove(s.typeName);
                }
                else
                {
                    list.Add(new UIPanelRegistryEntry
                    {
                        panelScriptTypeName = s.typeName,
                        prefabResourceId = s.prefabId,
                        destroyInstanceWhenClosed = true,
                        exclusiveFullscreen = false
                    });
                }
            }

            if (_keepEntriesNotInScan)
            {
                foreach (var kv in byType.OrderBy(k => k.Key, StringComparer.Ordinal))
                    list.Add(kv.Value);
            }

            next = list.ToArray();
        }

        registry.entries = next;
        EditorUtility.SetDirty(registry);
        serializedObject.Update();
        AssetDatabase.SaveAssets();

        Logger.Info($"[UIPanelRegistry] 扫描完成：目录 {folder}，条目 {next.Length}。", LogTag.Resource);
    }

    /// <summary>
    /// 优先根物体上的 UIPanel 派生组件；否则按层级深度优先取最浅的一个。
    /// </summary>
    static bool TryResolveConcreteUIPanelType(GameObject root, out Type panelType)
    {
        panelType = null;
        if (root == null)
            return false;

        MonoBehaviour pick = null;
        int bestDepth = int.MaxValue;

        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null)
                continue;
            Type t = mb.GetType();
            if (t == typeof(UIPanel) || !typeof(UIPanel).IsAssignableFrom(t) || t.IsAbstract)
                continue;

            int d = GetDepthUnderRoot(mb.transform, root.transform);
            if (d < bestDepth)
            {
                bestDepth = d;
                pick = mb;
            }
        }

        if (pick == null)
            return false;

        panelType = pick.GetType();
        return true;
    }

    /// <summary>节点相对 root 的层级深度：root 自身为 0。</summary>
    static int GetDepthUnderRoot(Transform node, Transform root)
    {
        int d = 0;
        Transform t = node;
        while (t != null)
        {
            if (t == root)
                return d;
            t = t.parent;
            d++;
        }

        return int.MaxValue;
    }
}
