#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageTimelineConfigViewer), true)]
public class StageTimelineConfigEditor : Editor
{
    SerializedProperty _previewWaveIndexProp;
    SerializedProperty _previewPathEntryIndexProp;
    SerializedProperty _drawPathGizmoProp;
    SerializedProperty _drawMidBossPathGizmoProp;
    SerializedProperty _drawMainBossPathGizmoProp;
    SerializedProperty _pathEditTargetProp;
    SerializedProperty _previewMidBossPathPhaseProp;
    SerializedProperty _previewMainBossPathPhaseProp;
    SerializedProperty _pathNodeSnapToGridProp;
    SerializedProperty _pathNodeSnapCellSizeProp;
    SerializedProperty _drawPathNodeSnapGridProp;

    void OnEnable()
    {
        _previewWaveIndexProp = serializedObject.FindProperty("previewMidStageWaveIndex");
        _previewPathEntryIndexProp = serializedObject.FindProperty("previewPathEditEntryIndex");
        _drawPathGizmoProp = serializedObject.FindProperty("drawWavePathGizmo");
        _drawMidBossPathGizmoProp = serializedObject.FindProperty("drawMidBossPathGizmo");
        _drawMainBossPathGizmoProp = serializedObject.FindProperty("drawMainBossPathGizmo");
        _pathEditTargetProp = serializedObject.FindProperty("pathEditTarget");
        _previewMidBossPathPhaseProp = serializedObject.FindProperty("previewMidBossPathPhase");
        _previewMainBossPathPhaseProp = serializedObject.FindProperty("previewMainBossPathPhase");
        _pathNodeSnapToGridProp = serializedObject.FindProperty("pathNodeSnapToGrid");
        _pathNodeSnapCellSizeProp = serializedObject.FindProperty("pathNodeSnapCellSize");
        _drawPathNodeSnapGridProp = serializedObject.FindProperty("drawPathNodeSnapGrid");
    }

    void OnDisable()
    {
        StageTimelineEmbeddedConfigEditor.Cleanup();
    }

    const string TimelineConfigField = "stageTimelineConfig";
    const string BattleAreaConfigField = "battleAreaConfig";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var viewer = (StageTimelineConfigViewer)target;
        var previousTimeline = viewer.stageTimelineConfig;
        var previousBattleArea = viewer.battleAreaConfig;

        var timelineRef = serializedObject.FindProperty(TimelineConfigField);
        bool timelineRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(
            timelineRef,
            new GUIContent("关卡时间轴配置"));

        var battleAreaRef = serializedObject.FindProperty(BattleAreaConfigField);
        bool battleAreaRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(
            battleAreaRef,
            new GUIContent("战斗区配置"));

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            TimelineConfigField,
            BattleAreaConfigField,
            "previewMidStageWaveIndex",
            "previewPathEditEntryIndex",
            "drawWavePathGizmo",
            "drawMidBossPathGizmo",
            "drawMainBossPathGizmo",
            "pathEditTarget",
            "previewMidBossPathPhase",
            "previewMainBossPathPhase",
            "pathNodeSnapToGrid",
            "pathNodeSnapCellSize",
            "drawPathNodeSnapGrid");

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.SyncViewerOnConfigReferenceChanged(
            viewer,
            previousTimeline,
            viewer.stageTimelineConfig,
            serializedObject,
            timelineRefChanged);

        if (battleAreaRefChanged && !serializedObject.isEditingMultipleObjects
            && viewer.battleAreaConfig != previousBattleArea)
        {
            viewer.StopAllEditorPreviews();
            EditorUtility.SetDirty(viewer);
            SceneView.RepaintAll();
        }

        serializedObject.Update();

        ConfigViewerEditorUI.DrawSeparator();
        DrawPathViewSection(viewer);

        ConfigViewerEditorUI.DrawSeparator();
        EditorGUILayout.LabelField("配置编辑（就地）", EditorStyles.boldLabel);
        DrawEmbeddedWaveConfig(viewer);
        DrawEmbeddedBossConfigs(viewer);

        ConfigViewerEditorUI.DrawSeparator();
        EditorGUILayout.LabelField("运行时预览（需 Play）", EditorStyles.boldLabel);

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.stageTimelineConfig, "StageTimelineConfig"))
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        DrawPendingPreviewRestart(viewer);
        DrawPreviewAvailability(viewer);
        DrawScopedPreviewControls(viewer);
        DrawFullTimelinePreview(viewer);
        DrawActivePreviewStatus(viewer);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawEmbeddedWaveConfig(StageTimelineConfigViewer viewer)
    {
        var timeline = viewer.stageTimelineConfig;
        if (timeline?.midStageWaves == null || timeline.midStageWaves.Count == 0)
        {
            EditorGUILayout.HelpBox("时间轴未配置道中波次。", MessageType.None);
            return;
        }

        int index = Mathf.Clamp(_previewWaveIndexProp?.intValue ?? 0, 0, timeline.midStageWaves.Count - 1);
        var wave = timeline.midStageWaves[index];
        using (new StageTimelinePathEditScope(viewer, E_StageTimelinePathEditTarget.MidStageWave, index))
        {
            StageTimelineEmbeddedConfigEditor.DrawScriptableObject(
                wave,
                viewer,
                $"道中波次 [{index}] · {wave?.name ?? "（空）"}",
                defaultExpanded: true);
        }
    }

    void DrawEmbeddedBossConfigs(StageTimelineConfigViewer viewer)
    {
        var timeline = viewer.stageTimelineConfig;
        if (timeline == null)
            return;

        using (new StageTimelinePathEditScope(viewer, E_StageTimelinePathEditTarget.MidBoss))
        {
            StageTimelineEmbeddedConfigEditor.DrawScriptableObject(
                timeline.midBossEncounter,
                viewer,
                "中场 Boss Encounter",
                defaultExpanded: false);
        }

        using (new StageTimelinePathEditScope(viewer, E_StageTimelinePathEditTarget.MainBoss))
        {
            StageTimelineEmbeddedConfigEditor.DrawScriptableObject(
                timeline.mainBossEncounter,
                viewer,
                "关底 Boss Encounter",
                defaultExpanded: false);
        }
    }

    static void DrawPendingPreviewRestart(StageTimelineConfigViewer viewer)
    {
        if (!viewer.HasPendingPreviewRestart)
            return;

        EditorGUILayout.HelpBox(
            "子配置已修改。Scene 路径 Gizmo 已更新；运行时预览需重启后才会应用新参数。",
            MessageType.Info);

        if (GUILayout.Button("应用修改并重启当前预览", GUILayout.Height(24)))
            viewer.RestartActivePreview();
    }

    void DrawPathViewSection(StageTimelineConfigViewer viewer)
    {
        EditorGUILayout.LabelField("Scene 路径查看", EditorStyles.boldLabel);

        if (_drawPathGizmoProp != null)
            EditorGUILayout.PropertyField(_drawPathGizmoProp, new GUIContent("绘制波次路径 Gizmo"));

        if (_drawMidBossPathGizmoProp != null)
            EditorGUILayout.PropertyField(_drawMidBossPathGizmoProp, new GUIContent("绘制中场 Boss 路径"));

        if (_drawMainBossPathGizmoProp != null)
            EditorGUILayout.PropertyField(_drawMainBossPathGizmoProp, new GUIContent("绘制关底 Boss 路径"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("路径点编辑", EditorStyles.boldLabel);
        if (_pathNodeSnapToGridProp != null)
            EditorGUILayout.PropertyField(_pathNodeSnapToGridProp, new GUIContent("网格吸附"));
        using (new EditorGUI.DisabledScope(_pathNodeSnapToGridProp != null && !_pathNodeSnapToGridProp.boolValue))
        {
            if (_pathNodeSnapCellSizeProp != null)
                EditorGUILayout.PropertyField(_pathNodeSnapCellSizeProp, new GUIContent("网格精度"));
            if (_drawPathNodeSnapGridProp != null)
                EditorGUILayout.PropertyField(_drawPathNodeSnapGridProp, new GUIContent("绘制吸附网格"));
        }

        var timeline = viewer.stageTimelineConfig;
        bool hasTimeline = timeline != null;
        bool hasWaves = hasTimeline && timeline.midStageWaves != null && timeline.midStageWaves.Count > 0;

        if (!hasTimeline)
        {
            EditorGUILayout.HelpBox("请指定 StageTimelineConfig。", MessageType.Warning);
            return;
        }

        if (!viewer.TryResolveGizmoBattleArea(out _))
        {
            EditorGUILayout.HelpBox(
                "请在 Viewer 上指定 Battle Area Config，以便在 Scene 中显示战斗区与路径（无需 Play）。",
                MessageType.Warning);
        }

        EditorGUI.BeginDisabledGroup(!hasWaves);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (hasWaves && _previewWaveIndexProp != null)
            {
                int max = timeline.midStageWaves.Count - 1;
                EditorGUI.BeginChangeCheck();
                _previewWaveIndexProp.intValue = EditorGUILayout.IntSlider(
                    new GUIContent("波次索引", "切换查看/编辑 midStageWaves 中的波次"),
                    Mathf.Clamp(_previewWaveIndexProp.intValue, 0, max),
                    0,
                    max);
                if (EditorGUI.EndChangeCheck())
                {
                    StageTimelineEmbeddedConfigEditor.Cleanup();
                    viewer.RepaintPathGizmo();
                }
            }
            else
            {
                EditorGUILayout.LabelField("波次索引", "无道中波次");
            }
        }
        EditorGUI.EndDisabledGroup();

        DrawPathEditTargetSection(viewer, timeline, hasWaves);

        EditorGUILayout.HelpBox(
            "Scene 绘制全部轨迹与路径点；当前阶段/条目高亮可编辑。"
            + " 波次：点击生成点或队列「路径」；Boss：点击阶段锚点切换。",
            MessageType.None);
    }

    void DrawPathEditTargetSection(StageTimelineConfigViewer viewer, StageTimelineConfig timeline, bool hasWaves)
    {
        if (_pathEditTargetProp == null)
            return;

        EditorGUILayout.Space(4);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_pathEditTargetProp, new GUIContent("路径编辑对象"));
        if (EditorGUI.EndChangeCheck())
            viewer.RepaintPathGizmo();

        var target = (E_StageTimelinePathEditTarget)_pathEditTargetProp.enumValueIndex;

        switch (target)
        {
            case E_StageTimelinePathEditTarget.MidBoss:
                DrawMidBossPathEditSection(viewer, timeline.midBossEncounter);
                break;
            case E_StageTimelinePathEditTarget.MainBoss:
                DrawMainBossPathEditSection(viewer, timeline.mainBossEncounter);
                break;
            default:
                if (!hasWaves)
                    EditorGUILayout.HelpBox("时间轴未配置 midStageWaves。", MessageType.None);
                else if (_previewWaveIndexProp != null)
                {
                    var wave = timeline.midStageWaves[_previewWaveIndexProp.intValue];
                    int pathEntry = _previewPathEntryIndexProp?.intValue ?? 0;
                    DrawWaveSummary(wave, _previewWaveIndexProp.intValue);
                    DrawPathEditEntrySlider(viewer, wave);
                    DrawActiveEntryPathRoute(viewer, wave, pathEntry);
                }

                break;
        }
    }

    static void DrawMidBossPathEditSection(StageTimelineConfigViewer viewer, MidBossEncounterConfig encounter)
    {
        if (encounter == null || !encounter.enabled)
        {
            EditorGUILayout.HelpBox("未配置或未启用 midBossEncounter。", MessageType.None);
            return;
        }

        DrawBossPathPhaseSlider(
            viewer.PreviewMidBossPathPhase,
            StageTimelineBossPathEdit.MidBossPhaseCount,
            StageTimelineBossPathEdit.GetMidBossPhaseLabel,
            viewer.SetMidBossPathPhase);

        StageTimelineBossPathEdit.EnsureMidBossRouteInitialized(encounter, viewer.PreviewMidBossPathPhase);
        DrawBossActivePathRoute(
            viewer,
            encounter,
            GetMidBossPathPropertyName(viewer.PreviewMidBossPathPhase),
            $"运动路径 · 中场 {StageTimelineBossPathEdit.GetMidBossPhaseLabel(viewer.PreviewMidBossPathPhase)}");
    }

    static void DrawMainBossPathEditSection(StageTimelineConfigViewer viewer, MainBossEncounterConfig encounter)
    {
        if (encounter == null || !encounter.enabled)
        {
            EditorGUILayout.HelpBox("未配置或未启用 mainBossEncounter。", MessageType.None);
            return;
        }

        DrawBossPathPhaseSlider(
            viewer.PreviewMainBossPathPhase,
            StageTimelineBossPathEdit.MainBossPhaseCount,
            StageTimelineBossPathEdit.GetMainBossPhaseLabel,
            viewer.SetMainBossPathPhase);

        StageTimelineBossPathEdit.EnsureMainBossRouteInitialized(encounter, viewer.PreviewMainBossPathPhase);
        DrawBossActivePathRoute(
            viewer,
            encounter,
            GetMainBossPathPropertyName(viewer.PreviewMainBossPathPhase),
            $"运动路径 · 关底 {StageTimelineBossPathEdit.GetMainBossPhaseLabel(viewer.PreviewMainBossPathPhase)}");
    }

    static void DrawBossPathPhaseSlider(
        int currentPhase,
        int phaseCount,
        System.Func<int, string> labelForPhase,
        System.Action<int> onPhaseChanged)
    {
        if (phaseCount <= 1)
            return;

        var labels = new string[phaseCount];
        for (int i = 0; i < phaseCount; i++)
            labels[i] = labelForPhase(i);

        EditorGUI.BeginChangeCheck();
        int next = GUILayout.Toolbar(Mathf.Clamp(currentPhase, 0, phaseCount - 1), labels);
        if (EditorGUI.EndChangeCheck())
            onPhaseChanged(next);
    }

    static string GetMidBossPathPropertyName(int phaseIndex) => phaseIndex switch
    {
        0 => nameof(MidBossEncounterConfig.entryPathRoute),
        1 => nameof(MidBossEncounterConfig.loopPathRoute),
        _ => nameof(MidBossEncounterConfig.exitPathRoute),
    };

    static string GetMainBossPathPropertyName(int phaseIndex) => phaseIndex switch
    {
        0 => nameof(MainBossEncounterConfig.entryPathRoute),
        _ => nameof(MainBossEncounterConfig.loopPathRoute),
    };

    static void DrawBossActivePathRoute(
        StageTimelineConfigViewer viewer,
        UnityEngine.Object encounter,
        string pathPropertyName,
        string title)
    {
        if (encounter == null || string.IsNullOrEmpty(pathPropertyName))
            return;

        var so = new SerializedObject(encounter);
        so.Update();
        SerializedProperty pathProp = so.FindProperty(pathPropertyName);
        if (pathProp == null)
            return;

        EditorGUILayout.Space(4);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(pathProp, new GUIContent(title), includeChildren: true);
        if (EditorGUI.EndChangeCheck())
        {
            so.ApplyModifiedProperties();
            viewer.OnEmbeddedConfigChanged();
        }
    }

    void DrawScopedPreviewControls(StageTimelineConfigViewer viewer)
    {
        var timeline = viewer.stageTimelineConfig;
        bool previewBlocked = !CanStartPreview(viewer);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("分段预览", EditorStyles.boldLabel);

        DrawMidStageWavePlayPreview(viewer, timeline, previewBlocked);
        DrawMidBossPreview(viewer, timeline, previewBlocked);
        DrawMainBossPreview(viewer, timeline, previewBlocked);
    }

    void DrawMidStageWavePlayPreview(StageTimelineConfigViewer viewer, StageTimelineConfig timeline, bool previewBlocked)
    {
        var waves = timeline.midStageWaves;
        bool hasWaves = waves != null && waves.Count > 0;

        EditorGUI.BeginDisabledGroup(previewBlocked || !hasWaves);
        if (GUILayout.Button("预览波次（运行）", GUILayout.Height(24)))
            viewer.RequestPreviewMidStageWave(_previewWaveIndexProp?.intValue ?? 0);
        EditorGUI.EndDisabledGroup();

        if (!hasWaves)
            EditorGUILayout.HelpBox("时间轴未配置 midStageWaves。", MessageType.None);
    }

    static void DrawPreviewAvailability(StageTimelineConfigViewer viewer)
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(StageTimelinePreviewRuntime.PlayModeRequiredMessage, MessageType.Warning);
            return;
        }

        if (!StageTimelinePreviewRuntime.CanPreview)
        {
            EditorGUILayout.HelpBox(StageTimelinePreviewRuntime.InBattleBlockedMessage, MessageType.Warning);
            return;
        }

        if (viewer.IsPreviewBootstrapping || StageTimelinePreviewRuntime.IsLoading)
            EditorGUILayout.HelpBox("正在加载预览资源…", MessageType.Info);
        else
        {
            ConfigViewerEditorUI.DrawPrefabSyncHint(
                "运行时预览使用独立 ECS World。修改配置后点「应用修改并重启当前预览」；路径 Gizmo 会即时刷新。");
        }
    }

    static void DrawWaveSummary(EnemyWaveConfig wave, int index)
    {
        if (wave == null)
        {
            EditorGUILayout.HelpBox($"波次 [{index}] 引用为空。", MessageType.Warning);
            return;
        }

        wave.EnsureSpawnQueueMigrated();
        int spawnN = wave.ResolveSpawnCount();
        string queueHint = wave.UsesSequentialSpawn ? " · 顺序" : "";
        string enemySummary = spawnN <= 0
            ? "（队列为空）"
            : spawnN == 1
                ? wave.spawnQueue[0].enemyConfigId
                : $"{spawnN} 名";
        EditorGUILayout.LabelField(
            $"[{index}] {wave.name} · {enemySummary} · {wave.spawnPattern}{queueHint}",
            EditorStyles.miniLabel);
    }

    void DrawPathEditEntrySlider(StageTimelineConfigViewer viewer, EnemyWaveConfig wave)
    {
        if (wave == null || _previewPathEntryIndexProp == null)
            return;

        wave.EnsureSpawnQueueMigrated();
        int count = wave.ResolveSpawnCount();
        if (count <= 1)
            return;

        string label = wave.UsesPerQueueEntryPaths
            ? "当前路径条目"
            : "路径锚定条目";
        string tooltip = wave.UsesPerQueueEntryPaths
            ? "Scene 与下方路径块对应该队列条目；点击 Scene 生成点或队列「路径」切换。"
            : "全队共用 pathRoute；Scene 按该条目的生成点展示与编辑路径。";

        EditorGUI.BeginChangeCheck();
        _previewPathEntryIndexProp.intValue = EditorGUILayout.IntSlider(
            new GUIContent(label, tooltip),
            Mathf.Clamp(_previewPathEntryIndexProp.intValue, 0, count - 1),
            0,
            count - 1);
        if (EditorGUI.EndChangeCheck())
            viewer.RepaintPathGizmo();
    }

    static void DrawActiveEntryPathRoute(StageTimelineConfigViewer viewer, EnemyWaveConfig wave, int entryIndex)
    {
        if (wave == null || viewer == null)
            return;

        entryIndex = wave.ResolvePathDisplayEntryIndex(entryIndex);
        if (wave.UsesPerQueueEntryPaths)
            wave.EnsureEntryPathOverrideInitialized(entryIndex);

        var waveSo = new SerializedObject(wave);
        waveSo.Update();

        SerializedProperty pathProp = wave.UsesPerQueueEntryPaths
            ? waveSo.FindProperty(nameof(EnemyWaveConfig.spawnQueue))
                .GetArrayElementAtIndex(entryIndex)
                .FindPropertyRelative(nameof(WaveSpawnQueueEntry.pathRouteOverride))
            : waveSo.FindProperty(nameof(EnemyWaveConfig.pathRoute));

        if (pathProp == null)
            return;

        EditorGUILayout.Space(4);
        string title = wave.UsesPerQueueEntryPaths
            ? $"运动路径 · 条目 #{entryIndex + 1}"
            : countLabel(wave, entryIndex);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(pathProp, new GUIContent(title), includeChildren: true);
        if (EditorGUI.EndChangeCheck())
        {
            waveSo.ApplyModifiedProperties();
            viewer.OnEmbeddedConfigChanged();
        }

        static string countLabel(EnemyWaveConfig w, int idx)
        {
            return w.ResolveSpawnCount() > 1
                ? $"运动路径 · 全队共享（锚定 #{idx + 1}）"
                : "运动路径 · 全队共享";
        }
    }

    static void DrawMidBossPreview(StageTimelineConfigViewer viewer, StageTimelineConfig timeline, bool previewBlocked)
    {
        var mid = timeline.midBossEncounter;
        bool canPreview = mid != null && mid.enabled && !string.IsNullOrEmpty(mid.enemyConfigId);

        EditorGUI.BeginDisabledGroup(previewBlocked || !canPreview);
        if (GUILayout.Button("预览中场 Boss（道中）", GUILayout.Height(24)))
            viewer.RequestPreviewMidBoss();
        EditorGUI.EndDisabledGroup();

        if (mid == null)
            EditorGUILayout.HelpBox("未引用 midBossEncounter。", MessageType.None);
        else if (!canPreview)
            EditorGUILayout.HelpBox("中场 Boss 未启用或未配置 enemyConfigId。", MessageType.None);
    }

    static void DrawMainBossPreview(StageTimelineConfigViewer viewer, StageTimelineConfig timeline, bool previewBlocked)
    {
        var main = timeline.mainBossEncounter;
        bool canPreview = main != null && main.enabled && !string.IsNullOrEmpty(main.enemyConfigId);

        EditorGUI.BeginDisabledGroup(previewBlocked || !canPreview);
        if (GUILayout.Button("预览关底 Boss", GUILayout.Height(24)))
            viewer.RequestPreviewMainBoss();
        EditorGUI.EndDisabledGroup();

        if (main == null)
            EditorGUILayout.HelpBox("未引用 mainBossEncounter。", MessageType.None);
        else if (!canPreview)
            EditorGUILayout.HelpBox("关底 Boss 未启用或未配置 enemyConfigId。", MessageType.None);
    }

    static void DrawFullTimelinePreview(StageTimelineConfigViewer viewer)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("完整时间轴", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(!CanStartPreview(viewer) || viewer.IsPreviewingTimeline);
        if (GUILayout.Button("预览完整关卡时间轴", GUILayout.Height(28)))
            viewer.RequestPreviewStageTimeline();
        EditorGUI.EndDisabledGroup();
    }

    static void DrawActivePreviewStatus(StageTimelineConfigViewer viewer)
    {
        if (!viewer.IsPreviewingTimeline)
            return;

        string scopeName = StageTimelineConfigViewer.GetPreviewScopeDisplayName(viewer.ActivePreviewScope);
        if (viewer.ActivePreviewScope == E_StageTimelinePreviewScope.SingleMidStageWave)
            scopeName += $" [{viewer.PreviewMidStageWaveIndex}]";

        EditorGUILayout.HelpBox(
            $"正在预览 {scopeName}：逻辑帧 {viewer.PreviewLogicFrame}，已播放 {viewer.PreviewElapsedSeconds:F1}s",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("停止预览", GUILayout.Height(24)))
                viewer.StopPreviewTimeline();
            if (viewer.HasPendingPreviewRestart
                && GUILayout.Button("重启预览", GUILayout.Height(24)))
                viewer.RestartActivePreview();
        }
    }

    static bool CanStartPreview(StageTimelineConfigViewer viewer) =>
        StageTimelinePreviewRuntime.CanPreview
        && !viewer.IsPreviewBootstrapping
        && !StageTimelinePreviewRuntime.IsLoading;
}
#endif
