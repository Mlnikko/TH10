using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DropItemConfigViewer), true)]
public class DropItemConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var viewer = (DropItemConfigViewer)target;

        ConfigViewerEditorUI.DrawSeparator();
        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.dropItemConfig, "DropItemConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("刷新 Sprite 预览", GUILayout.Height(28)))
            viewer.PreviewDropItem();

        if (GUILayout.Button("预览掉落运动", GUILayout.Height(28)))
            viewer.StartPreviewDropMotion();

        GUILayout.EndHorizontal();

        if (viewer.IsPreviewingDropMotion)
        {
            EditorGUILayout.HelpBox("正在预览掉落运动… 按逻辑帧模拟上抛与下落。", MessageType.Info);
            if (GUILayout.Button("停止运动预览", GUILayout.Height(24)))
                viewer.StopPreviewDropMotion();
        }

        ConfigViewerEditorUI.DrawSeparator();
        ConfigViewerEditorUI.DrawSaveButton(
            viewer.dropItemConfig,
            viewer.SaveDropItemConfig,
            "DropItemConfig");
    }
}
