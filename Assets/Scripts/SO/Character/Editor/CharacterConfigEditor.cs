#if UNITY_EDITOR
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

        ConfigViewerEditorUI.DrawPrefabSyncHint("双击进入角色预制体后自动同步角色参数。");

        DrawWeaponPreviewSection(viewer);

        ConfigViewerEditorUI.DrawSeparator();
        ConfigViewerEditorUI.DrawSaveButton(
            viewer.CharacterConfig,
            viewer.SaveCharacterConfig,
            "CharacterConfig");
    }

    static void DrawWeaponPreviewSection(CharacterConfigViewer viewer)
    {
        EditorGUILayout.LabelField("武器发射预览", EditorStyles.boldLabel);

        var characterCfg = viewer.CharacterConfig;
        if (characterCfg.weaponConfigIds == null || characterCfg.weaponConfigIds.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "请先在 CharacterConfig 资产中配置 weaponConfigIds（可选武器列表）。",
                MessageType.Warning);
            return;
        }

        int count = viewer.PreviewWeaponCount;
        var labels = new string[count];
        for (int i = 0; i < count; i++)
            labels[i] = viewer.GetPreviewWeaponLabel(i);

        SerializedObject so = new SerializedObject(viewer);
        SerializedProperty weaponIndexProp = so.FindProperty("previewWeaponIndex");
        SerializedProperty slowModeProp = so.FindProperty("previewUseSlowModePrimary");

        if (weaponIndexProp != null)
        {
            int picked = EditorGUILayout.Popup("预览武器", weaponIndexProp.intValue, labels);
            if (picked != weaponIndexProp.intValue)
                weaponIndexProp.intValue = picked;
        }

        if (slowModeProp != null)
            EditorGUILayout.PropertyField(slowModeProp, new GUIContent("预览低速主炮"));

        so.ApplyModifiedProperties();

        EditorGUILayout.HelpBox(
            "在 Scene 视图中将 Viewer 放在角色位置，预览该武器的主/副发射器弹幕（按逻辑帧间隔）。",
            MessageType.None);

        if (GUILayout.Button("预览武器发射", GUILayout.Height(28)))
            viewer.PreviewWeaponFire();

        if (viewer.IsPreviewingWeapon)
        {
            EditorGUILayout.HelpBox("正在预览武器发射…", MessageType.Info);
            if (GUILayout.Button("停止武器预览", GUILayout.Height(24)))
                viewer.StopWeaponPreview();
        }
    }
}
#endif
