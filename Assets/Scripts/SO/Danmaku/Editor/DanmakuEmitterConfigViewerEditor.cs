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

        var previousConfig = Viewer.emitterConfig;

        var configRef = serializedObject.FindProperty(EmitterConfigField);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(configRef, new GUIContent("配置文件"));
        bool emitterConfigRefChanged = EditorGUI.EndChangeCheck();

        DrawEmitterConfigResourceFields(Viewer);

        DrawPropertiesExcluding(serializedObject, "m_Script", EmitterConfigField);

        serializedObject.ApplyModifiedProperties();

        if (emitterConfigRefChanged && !serializedObject.isEditingMultipleObjects)
            SyncViewerWhenEmitterConfigChanged(Viewer, previousConfig);

        serializedObject.Update();

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    static void SyncViewerWhenEmitterConfigChanged(
        DanmakuEmitterConfigViewer viewer,
        DanmakuEmitterConfig previousConfig)
    {
        if (viewer == null || viewer.emitterConfig == previousConfig)
            return;

        viewer.StopAllEditorPreviews();
        if (viewer.emitterConfig != null)
            viewer.SyncFromConfigInEditor();

        EditorUtility.SetDirty(viewer);
        SceneView.RepaintAll();
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

        DrawSyncHint("切换配置文件或双击进入预制体编辑后，会自动从 DanmakuEmitterConfig 同步发射参数。");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("从配置加载", GUILayout.Height(28)))
            {
                Viewer.StopAllEditorPreviews();
                Viewer.LoadEmitterConfig();
                EditorUtility.SetDirty(Viewer);
                serializedObject.Update();
                SceneView.RepaintAll();
            }

            DrawSave(Viewer.emitterConfig, Viewer.SaveEmitterConfig, "DanmakuEmitterConfig");
        }

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

    }
}
#endif
