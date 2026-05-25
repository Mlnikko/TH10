#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DropItemConfigViewer), true)]
public class DropItemConfigViewerEditor : GameConfigViewerEditor<DropItemConfigViewer>
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        const string ConfigField = "dropItemConfig";
        const string PickupPrefabIdField = "pickupPrefabId";

        var configRef = serializedObject.FindProperty(ConfigField);
        if (configRef != null)
            EditorGUILayout.PropertyField(configRef);

        var prefabId = serializedObject.FindProperty(PickupPrefabIdField);
        if (prefabId != null)
            ResourceIdEditorPicker.DrawDropItemPrefabIdField(prefabId);

        DrawPropertiesExcluding(serializedObject, "m_Script", ConfigField, PickupPrefabIdField);

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    protected override void DrawViewerTools()
    {
        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        if (DrawMissingConfig(Viewer.dropItemConfig, "DropItemConfig"))
            return;

        DrawSyncHint();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新 Sprite 预览", GUILayout.Height(28)))
                Viewer.PreviewDropItem();

            if (GUILayout.Button("预览掉落运动", GUILayout.Height(28)))
                Viewer.StartPreviewDropMotion();
        }

        if (Viewer.IsPreviewingDropMotion)
        {
            EditorGUILayout.HelpBox("正在预览掉落运动…", MessageType.Info);
            if (GUILayout.Button("停止运动预览", GUILayout.Height(24)))
                Viewer.StopPreviewDropMotion();
        }

        ConfigViewerEditorUI.DrawSeparator();
        DrawSave(Viewer.dropItemConfig, Viewer.SaveDropItemConfig, "DropItemConfig");
    }
}
#endif
