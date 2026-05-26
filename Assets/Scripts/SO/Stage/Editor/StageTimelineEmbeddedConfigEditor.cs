#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在 <see cref="StageTimelineConfigViewer"/> Inspector 内嵌编辑子配置资产。
/// </summary>
static class StageTimelineEmbeddedConfigEditor
{
    static readonly Dictionary<int, Editor> s_editors = new();

    public static void DrawScriptableObject(
        ScriptableObject asset,
        StageTimelineConfigViewer viewer,
        string title,
        bool defaultExpanded = true)
    {
        if (asset == null)
        {
            EditorGUILayout.HelpBox($"{title} 未配置。", MessageType.Warning);
            return;
        }

        bool expanded = EditorPrefs.GetBool(PrefKey(asset), defaultExpanded);
        expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, title);
        EditorPrefs.SetBool(PrefKey(asset), expanded);
        if (!expanded)
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
            return;
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        Editor nested = GetOrCreateEditor(asset);
        if (nested == null)
            return;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        nested.OnInspectorGUI();
        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(asset);
            if (viewer != null && viewer.stageTimelineConfig != null)
                EditorUtility.SetDirty(viewer.stageTimelineConfig);

            viewer?.OnEmbeddedConfigChanged();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("定位资产", GUILayout.Width(80)))
                EditorGUIUtility.PingObject(asset);
            if (GUILayout.Button("单独 Inspector", GUILayout.Width(100)))
                Selection.activeObject = asset;
        }
    }

    static string PrefKey(Object asset) => $"TH10.StageTimeline.Embedded.{asset.GetInstanceID()}";

    static Editor GetOrCreateEditor(Object target)
    {
        int id = target.GetInstanceID();
        if (s_editors.TryGetValue(id, out Editor existing)
            && existing != null
            && existing.target == target)
            return existing;

        if (existing != null)
            Object.DestroyImmediate(existing);

        var editor = Editor.CreateEditor(target);
        s_editors[id] = editor;
        return editor;
    }

    public static void Cleanup()
    {
        foreach (var pair in s_editors)
        {
            if (pair.Value != null)
                Object.DestroyImmediate(pair.Value);
        }

        s_editors.Clear();
    }
}
#endif
