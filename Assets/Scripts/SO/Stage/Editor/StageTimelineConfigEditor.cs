#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageTimelineConfigViewer), true)]
public class StageTimelineConfigEditor : Editor
{
    SerializedProperty _previewWaveIndexProp;
    SerializedProperty _pathEditTargetProp;
    SerializedProperty _pathNodeSnapToGridProp;
    SerializedProperty _pathNodeSnapCellSizeProp;
    SerializedProperty _drawPathNodeSnapGridProp;
    SerializedProperty _previewDurationSecondsProp;

    const string PreviewDurationSecondsField = "previewDurationSeconds";

    void OnEnable()
    {
        _previewWaveIndexProp = serializedObject.FindProperty("previewMidStageWaveIndex");
        _pathEditTargetProp = serializedObject.FindProperty("pathEditTarget");
        _pathNodeSnapToGridProp = serializedObject.FindProperty("pathNodeSnapToGrid");
        _pathNodeSnapCellSizeProp = serializedObject.FindProperty("pathNodeSnapCellSize");
        _drawPathNodeSnapGridProp = serializedObject.FindProperty("drawPathNodeSnapGrid");
        _previewDurationSecondsProp = serializedObject.FindProperty(PreviewDurationSecondsField);
    }

    void OnDisable()
    {
        StageTimelineEmbeddedConfigEditor.Cleanup();
    }

    const string TimelineConfigField = "stageTimelineConfig";
    const string BattleAreaConfigField = "battleAreaConfig";
    const string PathNodeGridFoldoutPrefKey = "TH10.StageTimeline.PathNodeGridFoldout";
    const string StageBackgroundFoldoutPrefKey = "TH10.StageTimeline.StageBackgroundFoldout";
    const string WaveBossEditFoldoutPrefKey = "TH10.StageTimeline.WaveBossEditFoldout";
    const float StageBackgroundBoxInset = 8f;

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
            PreviewDurationSecondsField,
            "previewMidStageWaveIndex",
            "timelineViewDurationSeconds",
            "timelinePixelsPerSecond",
            "previewPathEditEntryIndex",
            "pathEditTarget",
            "previewMidBossPathPhase",
            "previewMainBossPathPhase",
            "previewMainBossSpellPhaseIndex",
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
        DrawRuntimePreviewSection(viewer);

        ConfigViewerEditorUI.DrawSeparator();
        if (viewer.stageTimelineConfig != null)
            StageTimelineVisualTimelineEditor.Draw(viewer, serializedObject);

        ConfigViewerEditorUI.DrawSeparator();
        DrawStageBackgroundSection(viewer);

        ConfigViewerEditorUI.DrawSeparator();
        DrawPathNodeGridSection(viewer);

        ConfigViewerEditorUI.DrawSeparator();
        DrawWaveAndBossEditSection(viewer);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawRuntimePreviewSection(StageTimelineConfigViewer viewer)
    {
        EditorGUILayout.LabelField("运行时预览（需 Play）", EditorStyles.boldLabel);

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.stageTimelineConfig, "StageTimelineConfig"))
            return;

        DrawPendingPreviewRestart(viewer);
        DrawPreviewAvailability(viewer);
        DrawPreviewDurationSettings(viewer);
        DrawScopedPreviewControls(viewer);
        DrawFullTimelinePreview(viewer);
        DrawActivePreviewStatus(viewer);
    }

    void DrawStageBackgroundSection(StageTimelineConfigViewer viewer)
    {
        bool expanded = EditorPrefs.GetBool(StageBackgroundFoldoutPrefKey, false);
        expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, "关卡背景");
        EditorPrefs.SetBool(StageBackgroundFoldoutPrefKey, expanded);
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (!expanded)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(StageBackgroundBoxInset);
                using (new EditorGUILayout.VerticalScope())
                {
                    if (viewer.stageTimelineConfig == null)
                    {
                        EditorGUILayout.HelpBox("请先指定关卡时间轴配置。", MessageType.Warning);
                        return;
                    }

                    var timelineSo = new SerializedObject(viewer.stageTimelineConfig);
                    timelineSo.Update();
                    var bgProp = timelineSo.FindProperty("backgroundData");
                    DrawSerializedPropertyChildren(bgProp, indent: true);
                    timelineSo.ApplyModifiedProperties();

                    EditorGUILayout.Space(4f);
                    DrawStageBackgroundPreviewControls(viewer);
                }
            }
        }
    }

    static void DrawSerializedPropertyChildren(SerializedProperty parent, bool indent = false)
    {
        if (parent == null)
            return;

        if (indent)
            EditorGUI.indentLevel++;

        SerializedProperty iterator = parent.Copy();
        SerializedProperty end = iterator.GetEndProperty();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            EditorGUILayout.PropertyField(iterator, true);
        }

        if (indent)
            EditorGUI.indentLevel--;
    }

    static void DrawStageBackgroundPreviewControls(StageTimelineConfigViewer viewer)
    {
        EditorGUILayout.LabelField("Scene 预览", EditorStyles.miniBoldLabel);

        bool canPreview = Application.isPlaying && StageTimelinePreviewRuntime.CanPreview;
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("背景预览需先进入 Play 模式（与运行时预览相同，会加载 GameResDB 并预热对象池）。", MessageType.Info);
        }
        else if (!StageTimelinePreviewRuntime.CanPreview)
        {
            EditorGUILayout.HelpBox(StageTimelinePreviewRuntime.InBattleBlockedMessage, MessageType.Warning);
        }

        EditorGUI.BeginDisabledGroup(!canPreview || viewer.IsBackgroundPreviewBootstrapping);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(viewer.IsBackgroundPreviewBootstrapping ? "启动中…" : "播放背景预览"))
                viewer.RequestBackgroundPreview();
            if (GUILayout.Button("停止背景预览"))
                viewer.StopBackgroundPreview();
        }

        EditorGUI.EndDisabledGroup();

        string status = viewer.IsBackgroundPreviewBootstrapping
            ? "启动中"
            : viewer.IsBackgroundPreviewActive ? "预览中" : "未播放";
        EditorGUILayout.LabelField($"状态：{status}", EditorStyles.miniLabel);
    }

    E_StageTimelinePathEditTarget GetPathEditTarget() =>
        _pathEditTargetProp != null
            ? (E_StageTimelinePathEditTarget)_pathEditTargetProp.enumValueIndex
            : E_StageTimelinePathEditTarget.MidStageWave;

    void DrawWaveAndBossEditSection(StageTimelineConfigViewer viewer)
    {
        bool expanded = EditorPrefs.GetBool(WaveBossEditFoldoutPrefKey, true);
        expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, "道中 / Boss 配置（Scene）");
        EditorPrefs.SetBool(WaveBossEditFoldoutPrefKey, expanded);
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (!expanded)
            return;

        var timeline = viewer.stageTimelineConfig;
        bool hasTimeline = timeline != null;
        bool hasWaves = hasTimeline && timeline.midStageWaves != null && timeline.midStageWaves.Count > 0;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
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

            DrawPathEditTargetSelector(viewer);

            EditorGUILayout.HelpBox(
                "路径可在 Scene 拖拽，也可在下方面板编辑。"
                + " 波次：点生成点或队列「路径」切换条目；Boss：切换路径阶段后编辑对应运动路径。",
                MessageType.None);

            switch (GetPathEditTarget())
            {
                case E_StageTimelinePathEditTarget.MidBoss:
                    DrawMidBossPathAndConfigBlock(viewer, timeline.midBossEncounter);
                    break;
                case E_StageTimelinePathEditTarget.MainBoss:
                    DrawMainBossPathAndConfigBlock(viewer, timeline.mainBossEncounter);
                    break;
                default:
                    DrawMidStageWavePathAndConfigBlock(viewer, timeline, hasWaves);
                    break;
            }
        }
    }

    void DrawPathEditTargetSelector(StageTimelineConfigViewer viewer)
    {
        if (_pathEditTargetProp == null)
            return;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(
            _pathEditTargetProp,
            new GUIContent("编辑对象", "决定 Scene 绘制、路径块与内嵌配置"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            StageTimelineEmbeddedConfigEditor.Cleanup();
            viewer.RepaintPathGizmo();
            SceneView.RepaintAll();
        }
    }

    void DrawMidStageWavePathAndConfigBlock(
        StageTimelineConfigViewer viewer,
        StageTimelineConfig timeline,
        bool hasWaves)
    {
        if (!hasWaves)
        {
            EditorGUILayout.HelpBox("时间轴未配置 midStageWaves。", MessageType.None);
            return;
        }

        if (_previewWaveIndexProp == null)
            return;

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

        int index = _previewWaveIndexProp.intValue;
        var wave = timeline.midStageWaves[index];

        DrawWaveSummary(wave, index);

        EditorGUILayout.Space(6f);
        DrawEmbeddedWaveConfig(viewer, timeline, index, wave);
    }

    void DrawMidBossPathAndConfigBlock(StageTimelineConfigViewer viewer, MidBossEncounterConfig encounter)
    {
        DrawMidBossPathEditSection(viewer, encounter);
        if (encounter == null || !encounter.enabled)
            return;

        EditorGUILayout.Space(6f);
        DrawEmbeddedMidBossConfig(viewer, encounter);
    }

    void DrawMainBossPathAndConfigBlock(StageTimelineConfigViewer viewer, MainBossEncounterConfig encounter)
    {
        DrawMainBossPathEditSection(viewer, encounter);
        if (encounter == null || !encounter.enabled)
            return;

        EditorGUILayout.Space(6f);
        DrawEmbeddedMainBossConfig(viewer, encounter);
    }

    void DrawEmbeddedWaveConfig(
        StageTimelineConfigViewer viewer,
        StageTimelineConfig timeline,
        int index,
        EnemyWaveConfig wave)
    {
        using (new StageTimelinePathEditScope(viewer, E_StageTimelinePathEditTarget.MidStageWave, index))
        {
            StageTimelineEmbeddedConfigEditor.DrawScriptableObject(
                wave,
                viewer,
                wave != null
                    ? $"波次参数 · {EnemyWaveConfig.FormatTimelineLabel(wave, index)}"
                    : $"波次参数 · 波次 {index}（空）",
                useFoldoutHeader: false);
        }
    }

    void DrawEmbeddedMidBossConfig(StageTimelineConfigViewer viewer, MidBossEncounterConfig encounter)
    {
        using (new StageTimelinePathEditScope(viewer, E_StageTimelinePathEditTarget.MidBoss))
        {
            StageTimelineEmbeddedConfigEditor.DrawScriptableObject(
                encounter,
                viewer,
                "中场 Boss 参数",
                useFoldoutHeader: false);
        }
    }

    void DrawEmbeddedMainBossConfig(StageTimelineConfigViewer viewer, MainBossEncounterConfig encounter)
    {
        using (new StageTimelinePathEditScope(viewer, E_StageTimelinePathEditTarget.MainBoss))
        {
            StageTimelineEmbeddedConfigEditor.DrawScriptableObject(
                encounter,
                viewer,
                "关底 Boss 参数",
                useFoldoutHeader: false);
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

    void DrawPathNodeGridSection(StageTimelineConfigViewer viewer)
    {
        bool expanded = EditorPrefs.GetBool(PathNodeGridFoldoutPrefKey, false);
        expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, "路径点网格（Scene）");
        EditorPrefs.SetBool(PathNodeGridFoldoutPrefKey, expanded);

        if (expanded)
        {
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_pathNodeSnapToGridProp != null)
                    EditorGUILayout.PropertyField(_pathNodeSnapToGridProp, new GUIContent("网格吸附"));
                using (new EditorGUI.DisabledScope(
                           _pathNodeSnapToGridProp != null && !_pathNodeSnapToGridProp.boolValue))
                {
                    if (_pathNodeSnapCellSizeProp != null)
                        EditorGUILayout.PropertyField(_pathNodeSnapCellSizeProp, new GUIContent("网格精度"));
                    if (_drawPathNodeSnapGridProp != null)
                        EditorGUILayout.PropertyField(_drawPathNodeSnapGridProp, new GUIContent("绘制吸附网格"));
                }
            }

            if (EditorGUI.EndChangeCheck())
                viewer.RepaintPathGizmo();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    static void DrawMidBossPathEditSection(StageTimelineConfigViewer viewer, MidBossEncounterConfig encounter)
    {
        DrawBossPathEditSection(
            encounter,
            boss => boss.enabled,
            "未配置或未启用 midBossEncounter。",
            viewer.PreviewMidBossPathPhase,
            StageTimelineBossPathEdit.MidBossPhaseCount,
            StageTimelineBossPathEdit.GetMidBossPhaseLabel,
            viewer.SetMidBossPathPhase,
            StageTimelineBossPathEdit.EnsureMidBossRouteInitialized);
    }

    static void DrawMainBossPathEditSection(StageTimelineConfigViewer viewer, MainBossEncounterConfig encounter)
    {
        if (!DrawBossPathEditSection(
                encounter,
                boss => boss.enabled,
                "未配置或未启用 mainBossEncounter。",
                viewer.PreviewMainBossPathPhase,
                StageTimelineBossPathEdit.MainBossPhaseCount,
                StageTimelineBossPathEdit.GetMainBossPhaseLabel,
                viewer.SetMainBossPathPhase,
                StageTimelineBossPathEdit.EnsureMainBossRouteInitialized))
        {
            return;
        }

        DrawMainBossSpellPhaseSection(viewer, encounter);
    }

    static void DrawMainBossSpellPhaseSection(StageTimelineConfigViewer viewer, MainBossEncounterConfig encounter)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("符卡阶段", EditorStyles.boldLabel);
        int spellPhaseIndex = viewer.PreviewMainBossSpellPhaseIndex;

        StageTimelineBossSpellPhaseEdit.DrawPhaseToolbar(
            encounter,
            spellPhaseIndex,
            viewer.SetMainBossSpellPhaseIndex);

        StageTimelineBossSpellPhaseEdit.DrawSelectedPhaseInspector(viewer, encounter, spellPhaseIndex);
        StageTimelineBossSpellPhaseEdit.DrawPhaseListControls(
            viewer,
            encounter,
            spellPhaseIndex,
            viewer.SetMainBossSpellPhaseIndex);

        bool previewBlocked = !CanStartPreview(viewer);
        bool canPreviewSpell = StageTimelineBossSpellPhaseEdit.GetPhaseCount(encounter) > 0;
        EditorGUI.BeginDisabledGroup(previewBlocked || !canPreviewSpell);
        if (GUILayout.Button(
                new GUIContent("预览当前符卡阶段", "跳过登场，直接进入 BossFight 并应用所选符卡弹幕"),
                GUILayout.Height(24)))
        {
            viewer.RequestPreviewMainBossSpellPhase(spellPhaseIndex);
        }
        EditorGUI.EndDisabledGroup();

        if (!canPreviewSpell)
            EditorGUILayout.HelpBox("添加至少一个符卡阶段后可预览。", MessageType.None);
    }

    static bool DrawBossPathEditSection<TEncounter>(
        TEncounter encounter,
        System.Func<TEncounter, bool> isEnabled,
        string unavailableMessage,
        int currentPhase,
        int phaseCount,
        System.Func<int, string> labelForPhase,
        System.Action<int> onPhaseChanged,
        System.Action<TEncounter, int> ensureRouteInitialized)
        where TEncounter : UnityEngine.Object
    {
        if (encounter == null || !isEnabled(encounter))
        {
            EditorGUILayout.HelpBox(unavailableMessage, MessageType.None);
            return false;
        }

        DrawBossPathPhaseSlider(currentPhase, phaseCount, labelForPhase, onPhaseChanged);

        int phase = Mathf.Clamp(currentPhase, 0, phaseCount - 1);
        ensureRouteInitialized(encounter, phase);
        return true;
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

    void DrawScopedPreviewControls(StageTimelineConfigViewer viewer)
    {
        var timeline = viewer.stageTimelineConfig;
        bool previewBlocked = !CanStartPreview(viewer);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("分段预览", EditorStyles.boldLabel);

        var waves = timeline?.midStageWaves;
        bool hasWaves = waves != null && waves.Count > 0;
        var mid = timeline?.midBossEncounter;
        bool canPreviewMidBoss = mid != null && mid.enabled && !string.IsNullOrEmpty(mid.enemyConfigId);
        var main = timeline?.mainBossEncounter;
        bool canPreviewMainBoss = main != null && main.enabled && !string.IsNullOrEmpty(main.enemyConfigId);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginDisabledGroup(previewBlocked || !hasWaves);
            if (GUILayout.Button(
                    new GUIContent("道中波次", "预览当前波次索引的 midStageWaves"),
                    GUILayout.Height(24)))
                viewer.RequestPreviewMidStageWave(_previewWaveIndexProp?.intValue ?? 0);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(previewBlocked || !canPreviewMidBoss);
            if (GUILayout.Button(
                    new GUIContent("中场 Boss", "预览 midBossEncounter"),
                    GUILayout.Height(24)))
                viewer.RequestPreviewMidBoss();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(previewBlocked || !canPreviewMainBoss);
            if (GUILayout.Button(
                    new GUIContent("关底 Boss", "预览 mainBossEncounter"),
                    GUILayout.Height(24)))
                viewer.RequestPreviewMainBoss();
            EditorGUI.EndDisabledGroup();
        }

        if (!hasWaves)
            EditorGUILayout.HelpBox("时间轴未配置 midStageWaves，无法预览道中波次。", MessageType.None);
        else if (mid == null)
            EditorGUILayout.HelpBox("未引用 midBossEncounter。", MessageType.None);
        else if (!canPreviewMidBoss)
            EditorGUILayout.HelpBox("中场 Boss 未启用或未配置 enemyConfigId。", MessageType.None);
        else if (main == null)
            EditorGUILayout.HelpBox("未引用 mainBossEncounter。", MessageType.None);
        else if (!canPreviewMainBoss)
            EditorGUILayout.HelpBox("关底 Boss 未启用或未配置 enemyConfigId。", MessageType.None);
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
        EditorGUILayout.LabelField(
            $"[{index}] {wave.ResolveDisplayTitle()}",
            EditorStyles.miniLabel);
    }

    void DrawPreviewDurationSettings(StageTimelineConfigViewer viewer)
    {
        if (_previewDurationSecondsProp == null)
            return;

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("预览时长", EditorStyles.miniBoldLabel);

        bool useAuto = _previewDurationSecondsProp.floatValue <= 0f;
        var scope = viewer.IsPreviewingTimeline
            ? viewer.ActivePreviewScope
            : E_StageTimelinePreviewScope.FullTimeline;
        int scopeIndex = scope switch
        {
            E_StageTimelinePreviewScope.SingleMidStageWave => viewer.PreviewMidStageWaveIndex,
            E_StageTimelinePreviewScope.MainBossSpellPhase => viewer.PreviewMainBossSpellPhaseIndex,
            _ => 0,
        };
        float resolved = viewer.GetResolvedPreviewDurationSeconds(scope, scopeIndex);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel("最长播放");
            bool newAuto = EditorGUILayout.ToggleLeft("自动", useAuto, GUILayout.Width(52f));
            if (newAuto != useAuto)
                _previewDurationSecondsProp.floatValue = newAuto ? 0f : 120f;

            EditorGUI.BeginDisabledGroup(_previewDurationSecondsProp.floatValue <= 0f);
            float manual = EditorGUILayout.Slider(
                Mathf.Max(5f, _previewDurationSecondsProp.floatValue),
                5f,
                600f);
            EditorGUI.EndDisabledGroup();

            if (_previewDurationSecondsProp.floatValue > 0f)
                _previewDurationSecondsProp.floatValue = manual;
        }

        if (_previewDurationSecondsProp.floatValue <= 0f)
            EditorGUILayout.LabelField($"自动估算：约 {resolved:0.#} s（随当前预览片段变化）", EditorStyles.miniLabel);
        else
            EditorGUILayout.LabelField($"固定上限：{_previewDurationSecondsProp.floatValue:0.#} s", EditorStyles.miniLabel);

        if (viewer.IsPreviewingTimeline)
        {
            float elapsed = viewer.PreviewElapsedSeconds;
            float total = viewer.ActivePreviewTotalDurationSeconds;
            float newTotal = EditorGUILayout.Slider(
                new GUIContent("本次播放总长", "预览进行中可加长，无需重启"),
                total,
                Mathf.Max(elapsed + 1f, 5f),
                600f);
            if (!Mathf.Approximately(newTotal, total))
                viewer.SetActivePreviewTotalDurationSeconds(newTotal);
        }
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
        {
            var waves = viewer.stageTimelineConfig?.midStageWaves;
            int wi = viewer.PreviewMidStageWaveIndex;
            if (waves != null && wi >= 0 && wi < waves.Count && waves[wi] != null)
                scopeName = EnemyWaveConfig.FormatTimelineLabel(waves[wi], wi);
            else
                scopeName += $" [{wi}]";
        }
        if (viewer.ActivePreviewScope == E_StageTimelinePreviewScope.MainBossSpellPhase)
            scopeName += $" [{viewer.PreviewMainBossSpellPhaseIndex}]";

        float total = viewer.ActivePreviewTotalDurationSeconds;
        string durationLine = total > 0f
            ? $"{viewer.PreviewElapsedSeconds:F1} / {total:F1} s"
            : $"{viewer.PreviewElapsedSeconds:F1} s";

        EditorGUILayout.HelpBox(
            $"正在预览 {scopeName}：逻辑帧 {viewer.PreviewLogicFrame}，{durationLine}",
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
