using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterConfigViewer))]
public class CharacterConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var viewer = (CharacterConfigViewer)target;

        ConfigViewerEditorUI.DrawSeparator();

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.CharacterConfig, "CharacterConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint();
        ConfigViewerEditorUI.DrawSaveButton(
            viewer.CharacterConfig,
            viewer.SaveCharacterConfig,
            "CharacterConfig");
    }
}
