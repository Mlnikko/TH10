using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 各 ConfigViewer 自定义 Inspector 的共用 UI。
/// </summary>
public static class ConfigViewerEditorUI
{
    public const string PrefabSyncHint = "双击进入预制体编辑后自动同步；修改后请保存写回资产。";

    public static void DrawSeparator()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    public static bool DrawMissingConfigWarning(UnityEngine.Object configAsset, string configTypeName)
    {
        if (configAsset != null)
            return false;

        EditorGUILayout.HelpBox(
            $"请指定 {configTypeName}；双击进入预制体编辑时会自动从资产同步字段。",
            MessageType.Warning);
        return true;
    }

    public static void DrawPrefabSyncHint(string message = null)
    {
        EditorGUILayout.HelpBox(message ?? PrefabSyncHint, MessageType.None);
    }

    public static void DrawSaveButton(
        UnityEngine.Object configAsset,
        Action save,
        string configTypeName,
        int height = 30)
    {
        string label = $"保存到 {configTypeName}";
        if (!GUILayout.Button(label, GUILayout.Height(height)))
            return;

        if (!EditorUtility.DisplayDialog("确认保存？", $"将覆盖 {configTypeName} 资产", "确定", "取消"))
            return;

        save?.Invoke();
        if (configAsset != null)
            EditorUtility.SetDirty(configAsset);
        AssetDatabase.SaveAssets();
    }
}
