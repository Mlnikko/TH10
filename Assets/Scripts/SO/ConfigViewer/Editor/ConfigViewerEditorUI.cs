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

    /// <summary>
    /// 绘制 Config SO 引用字段；在 <see cref="SerializedObject.ApplyModifiedProperties"/> 之前调用。
    /// </summary>
    public static bool DrawConfigReferenceProperty(
        SerializedProperty configProperty,
        GUIContent label = null)
    {
        if (configProperty == null)
            return false;

        EditorGUI.BeginChangeCheck();
        if (label != null)
            EditorGUILayout.PropertyField(configProperty, label);
        else
            EditorGUILayout.PropertyField(configProperty);

        return EditorGUI.EndChangeCheck();
    }

    /// <summary>
    /// Inspector 切换 Config 引用后：停止预览并从 SO 同步 Viewer 字段与表现。
    /// </summary>
    public static void SyncViewerOnConfigReferenceChanged(
        GameConfigViewerBase viewer,
        UnityEngine.Object previousConfig,
        UnityEngine.Object currentConfig,
        SerializedObject serializedObject,
        bool configReferenceChanged)
    {
        if (!configReferenceChanged
            || viewer == null
            || serializedObject == null
            || serializedObject.isEditingMultipleObjects)
            return;

        if (currentConfig == previousConfig)
            return;

        viewer.StopAllEditorPreviews();
        if (currentConfig != null)
            viewer.SyncFromConfigInEditor();

        EditorUtility.SetDirty(viewer);
        serializedObject.Update();
        SceneView.RepaintAll();
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
