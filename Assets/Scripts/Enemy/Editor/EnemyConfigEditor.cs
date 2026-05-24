using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyConfigViewer), true)]
public class EnemyConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var viewer = (EnemyConfigViewer)target;

        ConfigViewerEditorUI.DrawSeparator();

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.EnemyConfig, "EnemyConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint();
        ConfigViewerEditorUI.DrawSaveButton(
            viewer.EnemyConfig,
            viewer.SaveEnemyConfig,
            "EnemyConfig");
    }
}
