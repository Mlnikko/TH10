#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DropItemConfigViewer), true)]
public class DropItemConfigViewerEditor : GameConfigViewerEditor<DropItemConfigViewer>
{
    const string ConfigField = "dropItemConfig";
    const string PickupPrefabIdField = "pickupPrefabId";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var previousConfig = Viewer.dropItemConfig;

        var configRef = serializedObject.FindProperty(ConfigField);
        bool configRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(configRef);

        var prefabId = serializedObject.FindProperty(PickupPrefabIdField);
        if (prefabId != null)
            ResourceIdEditorPicker.DrawDropItemPrefabIdField(prefabId);

        DrawPropertiesExcluding(serializedObject, "m_Script", ConfigField, PickupPrefabIdField);

        serializedObject.ApplyModifiedProperties();
        ApplyConfigReferenceSync(previousConfig, Viewer.dropItemConfig, configRefChanged);

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    protected override void DrawViewerTools()
    {
        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        if (DrawMissingConfig(Viewer.dropItemConfig, "DropItemConfig"))
            return;

        DrawSyncHint("切换配置文件或双击进入预制体编辑后，会自动从 DropItemConfig 同步参数与 Sprite 预览。");

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
