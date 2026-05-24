using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuEmitterConfigViewer), true)]
public class DanmakuEmitterConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var viewer = (DanmakuEmitterConfigViewer)target;

        ConfigViewerEditorUI.DrawSeparator();
        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.emitterConfig, "DanmakuEmitterConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint("双击进入预制体编辑后自动同步发射参数。");

        if (GUILayout.Button("预览发射效果", GUILayout.Height(28)))
            viewer.PreviewEmitterEffect();

        if (viewer.IsPreviewingEmitter)
        {
            EditorGUILayout.HelpBox(
                "正在预览发射：按逻辑帧间隔生成弹幕并沿每帧速度位移。",
                MessageType.Info);

            if (GUILayout.Button("停止发射预览", GUILayout.Height(24)))
                viewer.StopPreviewEmitter();
        }

        ConfigViewerEditorUI.DrawSeparator();
        ConfigViewerEditorUI.DrawSaveButton(
            viewer.emitterConfig,
            viewer.SaveEmitterConfig,
            "DanmakuEmitterConfig");
    }
}
