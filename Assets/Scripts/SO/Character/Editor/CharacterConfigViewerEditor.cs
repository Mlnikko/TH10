#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterConfigViewer))]
public class CharacterConfigViewerEditor : GameConfigViewerEditor<CharacterConfigViewer>
{
    const string CharacterConfigField = "characterConfig";
    const string WeaponConfigIdsField = "weaponConfigIds";
    const string PreviewWeaponField = "previewWeaponConfigId";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var configRef = serializedObject.FindProperty(CharacterConfigField);
        EditorGUILayout.PropertyField(configRef, new GUIContent("配置文件"));

        DrawWeaponConfigSection();

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            CharacterConfigField,
            WeaponConfigIdsField,
            PreviewWeaponField);

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    void DrawWeaponConfigSection()
    {
        EditorGUI.indentLevel++;

        var weaponIds = serializedObject.FindProperty(WeaponConfigIdsField);
        if (weaponIds != null)
            ResourceIdEditorPicker.DrawWeaponConfigIdArray(weaponIds);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("武器预览", EditorStyles.boldLabel);

        var previewId = serializedObject.FindProperty(PreviewWeaponField);
        if (previewId != null)
        {
            ResourceIdEditorPicker.DrawWeaponConfigIdFieldFromAllowed(
                previewId,
                weaponIds,
                "预览武器 Config Id");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新武器预览", GUILayout.Height(24)))
                    Viewer.RefreshPreviewWeapon();
            }

            if (string.IsNullOrWhiteSpace(previewId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "选择预览武器后会在角色原点挂接武器预制体（仅 Prefab 编辑模式可见）。",
                    MessageType.None);
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);
    }

    protected override void DrawViewerTools()
    {
        if (DrawMissingConfig(Viewer.CharacterConfig, "CharacterConfig"))
            return;

        DrawSyncHint("双击进入角色预制体后自动同步；可选武器与预览武器会写入 CharacterConfig。");

        ConfigViewerEditorUI.DrawSeparator();
        DrawSave(Viewer.CharacterConfig, Viewer.SaveCharacterConfig, "CharacterConfig");
    }
}
#endif
