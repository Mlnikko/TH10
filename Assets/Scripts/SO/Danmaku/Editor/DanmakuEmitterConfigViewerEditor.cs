#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuEmitterConfigViewer), true)]
public class DanmakuEmitterConfigViewerEditor : GameConfigViewerEditor<DanmakuEmitterConfigViewer>
{
    const string EmitterConfigField = "emitterConfig";

    static readonly string[] ViewerEditorOnlyFields =
    {
        "previewDuration",
        "previewBulletLifetime",
        "previewSpinDuration",
        "drawPreviewSpawnGizmos",
    };

    static readonly string[] ConfigResourceFields =
    {
        nameof(DanmakuEmitterConfig.emitterPrefabId),
        nameof(DanmakuEmitterConfig.danmakuConfigIds),
        nameof(DanmakuEmitterConfig.danmakuSelectMode),
    };

    static readonly string[] ConfigPresentationNoteFields =
    {
        nameof(DanmakuEmitterConfig.presentationDescription),
    };

    static readonly string[] ConfigDisplayFields =
    {
        nameof(DanmakuEmitterConfig.displaySprite),
        nameof(DanmakuEmitterConfig.displaySelfSpinDegreesPerSecond),
        nameof(DanmakuEmitterConfig.displayScaleMin),
        nameof(DanmakuEmitterConfig.displayScaleMax),
        nameof(DanmakuEmitterConfig.displayScaleCyclesPerSecond),
    };

    static readonly string[] ConfigLaunchFields =
    {
        nameof(DanmakuEmitterConfig.emitterCamp),
        nameof(DanmakuEmitterConfig.emitterPosOffset),
        nameof(DanmakuEmitterConfig.emitterRotOffsetZ),
        nameof(DanmakuEmitterConfig.danmakuRotOffsetZ),
        nameof(DanmakuEmitterConfig.salvoAngleAdvanceDeg),
        nameof(DanmakuEmitterConfig.launchIntervalSeconds),
        nameof(DanmakuEmitterConfig.initialLaunchDelaySeconds),
        nameof(DanmakuEmitterConfig.launchCount),
        nameof(DanmakuEmitterConfig.launchSpeed),
        nameof(DanmakuEmitterConfig.audio_Fire),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var previousConfig = Viewer.emitterConfig;

        var configRef = serializedObject.FindProperty(EmitterConfigField);
        bool emitterConfigRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(
            configRef,
            new GUIContent("配置文件"));

        EditorGUI.BeginChangeCheck();
        DrawViewerEditorPreviewSettings();
        bool previewSettingsChanged = EditorGUI.EndChangeCheck();

        if (Viewer.emitterConfig != null)
            DrawEmitterConfigSections(Viewer);
        else
        {
            EditorGUILayout.HelpBox(
                "指定 DanmakuEmitterConfig 后可在下方编辑表现与发射参数，Scene 预览随修改即时更新。",
                MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();

        if (previewSettingsChanged && Viewer.IsPreviewingEmitter)
            Viewer.RefreshEmitterPreviewLive();

        if (emitterConfigRefChanged && !serializedObject.isEditingMultipleObjects)
        {
            Viewer.StopAllEditorPreviews();
            Viewer.SyncFromConfigInEditor();
            EditorUtility.SetDirty(Viewer);
            SceneView.RepaintAll();
        }
        else
        {
            ApplyConfigReferenceSync(previousConfig, Viewer.emitterConfig, emitterConfigRefChanged);
        }

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    void DrawViewerEditorPreviewSettings()
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("编辑器预览", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        for (int i = 0; i < ViewerEditorOnlyFields.Length; i++)
        {
            var prop = serializedObject.FindProperty(ViewerEditorOnlyFields[i]);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, true);
        }

        EditorGUI.indentLevel--;
    }

    static void DrawEmitterConfigSections(DanmakuEmitterConfigViewer viewer)
    {
        var config = viewer.emitterConfig;
        var configSo = new SerializedObject(config);
        configSo.Update();

        EditorGUI.BeginChangeCheck();
        DrawResourceSection(config, configSo);
        DrawPresentationSection(config, configSo);
        DrawEmitModeSection(configSo);
        DrawLaunchSection(config, configSo);
        bool changed = EditorGUI.EndChangeCheck();

        if (configSo.ApplyModifiedProperties() | changed)
        {
            EditorUtility.SetDirty(config);
            viewer.SyncFromConfigInEditor();
            SceneView.RepaintAll();
        }
    }

    static void DrawResourceSection(DanmakuEmitterConfig config, SerializedObject configSo)
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("资源引用", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var prefabId = configSo.FindProperty(nameof(DanmakuEmitterConfig.emitterPrefabId));
        var danmakuIds = configSo.FindProperty(nameof(DanmakuEmitterConfig.danmakuConfigIds));
        var selectMode = configSo.FindProperty(nameof(DanmakuEmitterConfig.danmakuSelectMode));

        ResourceIdEditorPicker.DrawPrefabIdField(
            prefabId,
            nameof(GameResourceManifest.danmakuEmitterPrefabIds),
            "Prefabs/DanmakuEmitter");
        ResourceIdEditorPicker.DrawDanmakuConfigIdArray(danmakuIds);
        if (selectMode != null)
            EditorGUILayout.PropertyField(selectMode, new GUIContent("弹幕选择模式"));

        EditorGUI.indentLevel--;
    }

    static void DrawPresentationSection(DanmakuEmitterConfig config, SerializedObject configSo)
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("表现预览", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        DrawConfigProperties(configSo, ConfigPresentationNoteFields);
        DanmakuEmitterModeInspectorUI.DrawDisplayPreviewStatus(config);
        DrawConfigProperties(configSo, ConfigDisplayFields);

        EditorGUI.indentLevel--;
    }

    static void DrawEmitModeSection(SerializedObject configSo)
    {
        EditorGUILayout.Space(2);
        DanmakuEmitterModeInspectorUI.DrawEmitModeSection(
            configSo,
            DanmakuEmitterModeInspectorUI.ConfigEmitModeField);
    }

    static void DrawLaunchSection(DanmakuEmitterConfig config, SerializedObject configSo)
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("发射参数", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var campProp = configSo.FindProperty(nameof(DanmakuEmitterConfig.emitterCamp));
        if (campProp != null)
            EditorGUILayout.PropertyField(campProp, true);
        DanmakuEmitterModeInspectorUI.DrawAimAtPlayerIfEnemy(configSo);

        for (int i = 0; i < ConfigLaunchFields.Length; i++)
        {
            if (ConfigLaunchFields[i] == nameof(DanmakuEmitterConfig.emitterCamp)
                || ConfigLaunchFields[i] == DanmakuEmitterModeInspectorUI.ConfigAimAtPlayerField)
                continue;

            var prop = configSo.FindProperty(ConfigLaunchFields[i]);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, true);
        }

        if (config != null && config.launchCount == 0)
        {
            EditorGUILayout.HelpBox(
                "launchCount 为 0 时不会发射；保存 SO 时会自动改为 -1（无限齐射）。",
                MessageType.Warning);
        }

        EditorGUI.indentLevel--;
    }

    static void DrawConfigProperties(SerializedObject configSo, string[] propertyNames)
    {
        for (int i = 0; i < propertyNames.Length; i++)
        {
            var prop = configSo.FindProperty(propertyNames[i]);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, true);
        }
    }

    protected override void DrawViewerTools()
    {
        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        if (DrawMissingConfig(Viewer.emitterConfig, "DanmakuEmitterConfig"))
            return;

        DrawSyncHint(
            "表现与发射参数在 DanmakuEmitterConfig 上编辑，Scene 中 Sprite 随修改即时更新。"
            + "下方按钮可预览齐射轨迹与自转/缩放；修改后点保存将 Viewer 中编辑器项与 SO 对齐。");

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
                "正在预览发射：按逻辑帧间隔生成弹幕；调节 Inspector 参数会即时更新轨迹与后续齐射（无需停止）。",
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
