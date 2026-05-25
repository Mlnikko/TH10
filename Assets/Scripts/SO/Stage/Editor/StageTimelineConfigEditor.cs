#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageTimelineConfigViewer), true)]
public class StageTimelineConfigEditor : Editor
{
    SerializedProperty _previewWaveIndexProp;

    void OnEnable()
    {
        _previewWaveIndexProp = serializedObject.FindProperty("previewMidStageWaveIndex");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "previewMidStageWaveIndex");

        var viewer = (StageTimelineConfigViewer)target;

        ConfigViewerEditorUI.DrawSeparator();
        EditorGUILayout.LabelField("关卡时间轴预览（仅编辑器）", EditorStyles.boldLabel);

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.stageTimelineConfig, "StageTimelineConfig"))
            return;

        DrawPreviewAvailability(viewer);
        DrawScopedPreviewControls(viewer);
        DrawFullTimelinePreview(viewer);
        DrawActivePreviewStatus(viewer);
        DrawSceneGizmoLegend(viewer);

        serializedObject.ApplyModifiedProperties();
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
                "手动预览使用独立 ECS World，不影响正常进战。可预览完整时间轴，或单独预览波次 / 中场 Boss / 关底 Boss。");
        }
    }

    void DrawScopedPreviewControls(StageTimelineConfigViewer viewer)
    {
        var timeline = viewer.stageTimelineConfig;
        bool previewBlocked = !CanStartPreview(viewer);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("分段预览", EditorStyles.boldLabel);

        DrawMidStageWavePreview(viewer, timeline, previewBlocked);
        DrawMidBossPreview(viewer, timeline, previewBlocked);
        DrawMainBossPreview(viewer, timeline, previewBlocked);
    }

    void DrawMidStageWavePreview(StageTimelineConfigViewer viewer, StageTimelineConfig timeline, bool previewBlocked)
    {
        var waves = timeline.midStageWaves;
        bool hasWaves = waves != null && waves.Count > 0;

        EditorGUI.BeginDisabledGroup(previewBlocked || !hasWaves);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (hasWaves && _previewWaveIndexProp != null)
            {
                int max = waves.Count - 1;
                _previewWaveIndexProp.intValue = EditorGUILayout.IntSlider(
                    new GUIContent("波次索引", "分段预览与 Scene 路径 Gizmo 共用"),
                    Mathf.Clamp(_previewWaveIndexProp.intValue, 0, max),
                    0,
                    max);
            }
            else
            {
                EditorGUILayout.LabelField("波次索引", "无道中波次");
            }

            if (GUILayout.Button("预览波次", GUILayout.Height(24), GUILayout.Width(100)))
                viewer.RequestPreviewMidStageWave(_previewWaveIndexProp?.intValue ?? 0);
        }
        EditorGUI.EndDisabledGroup();

        if (!hasWaves)
            EditorGUILayout.HelpBox("时间轴未配置 midStageWaves。", MessageType.None);
        else if (_previewWaveIndexProp != null)
            DrawWaveSummary(waves[_previewWaveIndexProp.intValue], _previewWaveIndexProp.intValue);
    }

    static void DrawWaveSummary(EnemyWaveConfig wave, int index)
    {
        if (wave == null)
        {
            EditorGUILayout.HelpBox($"波次 [{index}] 引用为空。", MessageType.Warning);
            return;
        }

        string enemy = string.IsNullOrEmpty(wave.enemyConfigId) ? "（未指定敌人）" : wave.enemyConfigId;
        int spawnN = wave.ResolveSpawnCount();
        string queueHint = wave.UsesSequentialSpawn ? " · 顺序出怪" : "";
        EditorGUILayout.LabelField(
            $"[{index}] {wave.name} · {enemy} ×{spawnN} · {wave.spawnPattern}{queueHint}",
            EditorStyles.miniLabel);
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
        else
        {
            EditorGUILayout.LabelField(
                $"{mid.name} · {mid.enemyConfigId} · 登场 {mid.spawnTimeSeconds:F1}s",
                EditorStyles.miniLabel);
        }
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
        else
        {
            int phaseCount = main.bossPhases?.Count ?? 0;
            EditorGUILayout.LabelField(
                $"{main.name} · {main.enemyConfigId} · 登场 {main.spawnTimeSeconds:F1}s · {phaseCount} 阶段",
                EditorStyles.miniLabel);
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
            scopeName += $" [{viewer.PreviewMidStageWaveIndex}]";

        EditorGUILayout.HelpBox(
            $"正在预览 {scopeName}：逻辑帧 {viewer.PreviewLogicFrame}，已播放 {viewer.PreviewElapsedSeconds:F1}s",
            MessageType.Info);

        if (GUILayout.Button("停止预览", GUILayout.Height(24)))
            viewer.StopPreviewTimeline();
    }

    static bool CanStartPreview(StageTimelineConfigViewer viewer) =>
        StageTimelinePreviewRuntime.CanPreview
        && !viewer.IsPreviewBootstrapping
        && !StageTimelinePreviewRuntime.IsLoading;

    static void DrawSceneGizmoLegend(StageTimelineConfigViewer viewer)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Scene 可视化", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "选中本 Viewer 且在 Scene 视图可见时：绿色=战斗区，红色=GO 回收区；"
            + "黄色球=生成点，青色折线=运动路径，品红球=离开回收区（退场）位置。"
            + "分段预览区的「波次索引」滑动条同时决定 Scene 中绘制哪一条 midStageWaves。",
            MessageType.None);
    }
}
#endif
