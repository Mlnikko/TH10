#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuEmitterConfigViewer), true)]
public class DanmakuEmitterConfigViewerEditor : GameConfigViewerEditor<DanmakuEmitterConfigViewer>
{
    const string EmitterConfigField = "emitterConfig";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var configRef = serializedObject.FindProperty(EmitterConfigField);
        EditorGUILayout.PropertyField(configRef, new GUIContent("配置文件"));

        DrawEmitterConfigResourceFields(Viewer);

        DrawPropertiesExcluding(serializedObject, "m_Script", EmitterConfigField);

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    static void DrawEmitterConfigResourceFields(DanmakuEmitterConfigViewer viewer)
    {
        var config = viewer != null ? viewer.emitterConfig : null;
        if (config == null)
        {
            EditorGUILayout.HelpBox("指定 DanmakuEmitterConfig 后可配置预制体 Id 与装填弹幕。", MessageType.None);
            return;
        }

        var cfgSo = new SerializedObject(config);
        cfgSo.Update();

        var prefabId = cfgSo.FindProperty(nameof(DanmakuEmitterConfig.emitterPrefabId));
        var danmakuIds = cfgSo.FindProperty(nameof(DanmakuEmitterConfig.danmakuConfigIds));
        var selectMode = cfgSo.FindProperty(nameof(DanmakuEmitterConfig.danmakuSelectMode));

        EditorGUILayout.LabelField("资源引用", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        ResourceIdEditorPicker.DrawPrefabIdField(
            prefabId,
            nameof(GameResourceManifest.danmakuEmitterPrefabIds),
            "Prefabs/DanmakuEmitter");
        ResourceIdEditorPicker.DrawDanmakuConfigIdArray(danmakuIds);
        EditorGUILayout.PropertyField(selectMode, new GUIContent("弹幕选择模式"));
        EditorGUI.indentLevel--;

        if (cfgSo.ApplyModifiedProperties())
        {
            ConfigViewerPrefabSync.ApplyDanmakuEmitterDisplaySprite(config);
            viewer.SyncDisplaySpriteFromConfig();
        }
    }

    protected override void DrawViewerTools()
    {
        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        if (DrawMissingConfig(Viewer.emitterConfig, "DanmakuEmitterConfig"))
            return;

        DrawSyncHint("双击进入预制体编辑后自动同步发射参数。");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("预览发射效果", GUILayout.Height(28)))
                Viewer.PreviewEmitterEffect();

            if (GUILayout.Button("预览自转/缩放", GUILayout.Height(28)))
                Viewer.StartPreviewDisplaySpin();
        }

        if (Viewer.IsPreviewingEmitter)
        {
            EditorGUILayout.HelpBox(
                "正在预览发射：按逻辑帧间隔生成弹幕并沿每帧速度位移。",
                MessageType.Info);

            if (GUILayout.Button("停止发射预览", GUILayout.Height(24)))
                Viewer.StopPreviewEmitter();
        }

        if (Viewer.IsPreviewingDisplaySpin)
        {
            EditorGUILayout.HelpBox("正在预览 Sprite 自转/循环缩放…", MessageType.Info);
            if (GUILayout.Button("停止表现预览", GUILayout.Height(24)))
                Viewer.StopPreviewDisplaySpin();
        }

        ConfigViewerEditorUI.DrawSeparator();
        DrawSave(Viewer.emitterConfig, Viewer.SaveEmitterConfig, "DanmakuEmitterConfig");
    }
}
#endif
