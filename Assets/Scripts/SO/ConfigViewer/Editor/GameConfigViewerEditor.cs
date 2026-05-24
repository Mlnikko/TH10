#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 挂有 <see cref="GameConfigViewerBase"/> 的预制体 Inspector 基类。
/// </summary>
public abstract class GameConfigViewerEditor<TViewer> : Editor
    where TViewer : GameConfigViewerBase
{
    protected TViewer Viewer => (TViewer)target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    protected abstract void DrawViewerTools();

    protected bool DrawMissingConfig(UnityEngine.Object configAsset, string configTypeName)
    {
        return ConfigViewerEditorUI.DrawMissingConfigWarning(configAsset, configTypeName);
    }

    protected void DrawSyncHint(string message = null) =>
        ConfigViewerEditorUI.DrawPrefabSyncHint(message);

    protected void DrawSave(UnityEngine.Object configAsset, Action save, string configTypeName) =>
        ConfigViewerEditorUI.DrawSaveButton(configAsset, save, configTypeName);
}
#endif
