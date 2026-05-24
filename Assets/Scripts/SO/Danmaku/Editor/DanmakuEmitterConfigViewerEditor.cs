#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuEmitterConfigViewer), true)]
public class DanmakuEmitterConfigViewerEditor : GameConfigViewerEditor<DanmakuEmitterConfigViewer>
{
    protected override void DrawViewerTools()
    {
        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        if (DrawMissingConfig(Viewer.emitterConfig, "DanmakuEmitterConfig"))
            return;

        DrawSyncHint("双击进入预制体编辑后自动同步发射参数。");

        if (GUILayout.Button("预览发射效果", GUILayout.Height(28)))
            Viewer.PreviewEmitterEffect();

        if (Viewer.IsPreviewingEmitter)
        {
            EditorGUILayout.HelpBox(
                "正在预览发射：按逻辑帧间隔生成弹幕并沿每帧速度位移。",
                MessageType.Info);

            if (GUILayout.Button("停止发射预览", GUILayout.Height(24)))
                Viewer.StopPreviewEmitter();
        }

        ConfigViewerEditorUI.DrawSeparator();
        DrawSave(Viewer.emitterConfig, Viewer.SaveEmitterConfig, "DanmakuEmitterConfig");
    }
}
#endif
