#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterConfigViewer))]
public class CharacterConfigViewerEditor : GameConfigViewerEditor<CharacterConfigViewer>
{
    const string ConfigField = "characterConfig";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var previousConfig = Viewer.CharacterConfig;

        var configRef = serializedObject.FindProperty(ConfigField);
        bool configRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(configRef);

        DrawPropertiesExcluding(serializedObject, "m_Script", ConfigField);

        serializedObject.ApplyModifiedProperties();
        ApplyConfigReferenceSync(previousConfig, Viewer.CharacterConfig, configRefChanged);

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    protected override void DrawViewerTools()
    {
        if (DrawMissingConfig(Viewer.CharacterConfig, "CharacterConfig"))
            return;

        DrawSyncHint("切换配置文件或双击进入预制体编辑后，会自动从 CharacterConfig 同步参数。");

        ConfigViewerEditorUI.DrawSeparator();
        DrawSave(Viewer.CharacterConfig, Viewer.SaveCharacterConfig, "CharacterConfig");
    }
}
#endif
