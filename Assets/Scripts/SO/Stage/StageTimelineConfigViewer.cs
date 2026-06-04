using System;
using UnityEngine;

#if UNITY_EDITOR
using System.Threading.Tasks;
using UnityEditor;
#endif

/// <summary>
/// 战斗场景编辑器工具：Play 模式下手动预览 <see cref="StageTimelineConfig"/>，不参与正式游戏流程。
/// </summary>
public class StageTimelineConfigViewer : GameConfigViewerBase
{
    protected override bool AllowPlayModeExecution => true;

    protected override bool HasAssignedConfig => stageTimelineConfig != null;

#if UNITY_EDITOR
    const string BackgroundPreviewRuntimeName = "StageTimelineBackgroundPreviewRuntime";
#endif

    [Header("配置文件")]
    public StageTimelineConfig stageTimelineConfig;

    [Header("战斗区（用于刷怪坐标与回收边界）")]
    [Tooltip("留空则在预览时从 GameResourceManifest.battleAreaConfigId 读取")]
    public BattleAreaConfig battleAreaConfig;

#if UNITY_EDITOR
    [Header("编辑器预览")]
    [Tooltip("预览时长（秒）；≤0 时按片段自动估算或使用关卡 maxStageDurationSeconds")]
    [SerializeField] float previewDurationSeconds = 120f;

    [Header("可视化时间线")]
    [Tooltip("Inspector 时间轴显示的总时长（秒）；≤0 时使用 StageTimelineConfig.maxStageDurationSeconds 或按内容自动估算")]
    [SerializeField] float timelineViewDurationSeconds;

    [SerializeField, Range(4f, 48f)] float timelinePixelsPerSecond = 10f;

    [SerializeField] int previewMidStageWaveIndex;

    [Tooltip("Scene 路径编辑/高亮所对应的出怪队列条目索引")]
    [SerializeField] int previewPathEditEntryIndex;

    [Tooltip("当前 Scene 路径编辑对象")]
    [SerializeField] E_StageTimelinePathEditTarget pathEditTarget = E_StageTimelinePathEditTarget.MidStageWave;

    [Tooltip("中场 Boss 路径阶段：0=入场 1=循环 2=退场")]
    [SerializeField] int previewMidBossPathPhase;

    [Tooltip("关底 Boss 路径阶段：0=登场 1=场内")]
    [SerializeField] int previewMainBossPathPhase;

    [Tooltip("关底 Boss 符卡阶段索引（bossPhases 列表）")]
    [SerializeField] int previewMainBossSpellPhaseIndex;

    [Header("路径编辑（Scene）")]
    [Tooltip("拖拽路径点时吸附到以战斗区中心为原点的世界网格")]
    [SerializeField] bool pathNodeSnapToGrid = true;

    [Min(0.01f)]
    [Tooltip("路径点网格步长（世界单位，以战斗区 Center 为网格原点）")]
    [SerializeField] float pathNodeSnapCellSize = 0.25f;

    [Tooltip("在 Scene 中绘制吸附参考网格")]
    [SerializeField] bool drawPathNodeSnapGrid = true;

    bool _previewActive;
    E_StageTimelinePreviewScope _activePreviewScope = E_StageTimelinePreviewScope.FullTimeline;
    bool _previewBootstrapping;
    int _bootstrapGeneration;
    LogicFramePreviewRunner _previewClock;
    World _previewWorld;
    StageTimelineSystem _timelineSystem;
    uint _previewLogicFrame;
#endif

    public void LoadStageTimelineConfig() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (stageTimelineConfig == null)
        {
            Logger.Warn("[StageTimelineConfigViewer] 未指定 StageTimelineConfig。", LogTag.Config);
            return;
        }

        Logger.Debug($"[StageTimelineConfigViewer] 已加载 {stageTimelineConfig.name}", LogTag.Config);
    }

#if UNITY_EDITOR
    public bool IsPreviewingTimeline => _previewActive;
    public bool IsPreviewBootstrapping => _previewBootstrapping;
    public E_StageTimelinePreviewScope ActivePreviewScope => _activePreviewScope;
    public int PreviewMidStageWaveIndex => previewMidStageWaveIndex;
    public int PreviewPathEditEntryIndex => previewPathEditEntryIndex;
    public E_StageTimelinePathEditTarget PathEditTarget => pathEditTarget;
    public int PreviewMidBossPathPhase => previewMidBossPathPhase;
    public int PreviewMainBossPathPhase => previewMainBossPathPhase;
    public int PreviewMainBossSpellPhaseIndex => previewMainBossSpellPhaseIndex;
    public bool PathNodeSnapToGrid => pathNodeSnapToGrid;
    public float PathNodeSnapCellSize => pathNodeSnapCellSize;
    public bool DrawPathNodeSnapGrid => drawPathNodeSnapGrid;
    public float TimelineViewDurationSeconds => timelineViewDurationSeconds;
    public float TimelinePixelsPerSecond => timelinePixelsPerSecond;
    public float PreviewDurationSeconds => previewDurationSeconds;

    public float GetResolvedPreviewDurationSeconds(
        E_StageTimelinePreviewScope scope = E_StageTimelinePreviewScope.FullTimeline,
        int previewIndex = 0)
        => ResolvePreviewDurationSeconds(scope, previewIndex);

    public void SetActivePreviewTotalDurationSeconds(float totalSeconds)
    {
        if (!_previewActive || totalSeconds <= 0f)
            return;

        _previewClock.SetTotalMaxRealSeconds(totalSeconds);
        EditorUtility.SetDirty(this);
    }

    public void SetPreviewMidStageWaveIndex(int waveIndex)
    {
        var waves = stageTimelineConfig?.midStageWaves;
        if (waves == null || waves.Count == 0)
            return;

        int clamped = Mathf.Clamp(waveIndex, 0, waves.Count - 1);
        if (previewMidStageWaveIndex == clamped)
            return;

        previewMidStageWaveIndex = clamped;
        pathEditTarget = E_StageTimelinePathEditTarget.MidStageWave;
        EditorUtility.SetDirty(this);
        RepaintPathGizmo();
    }

    public void SetPreviewPathEditEntryIndex(int entryIndex)
    {
        var waves = stageTimelineConfig?.midStageWaves;
        int waveIndex = ResolveGizmoWaveIndex();
        if (waves == null || waveIndex < 0 || waveIndex >= waves.Count)
            return;

        var wave = waves[waveIndex];
        if (wave == null)
            return;

        wave.EnsureSpawnQueueMigrated();
        int max = Mathf.Max(0, wave.ResolveSpawnCount() - 1);
        int clamped = Mathf.Clamp(entryIndex, 0, max);
        if (previewPathEditEntryIndex == clamped)
            return;

        previewPathEditEntryIndex = clamped;
        pathEditTarget = E_StageTimelinePathEditTarget.MidStageWave;
        EditorUtility.SetDirty(this);
        RepaintPathGizmo();
    }

    public void SetPathEditTarget(E_StageTimelinePathEditTarget target)
    {
        if (pathEditTarget == target)
            return;

        pathEditTarget = target;
        EditorUtility.SetDirty(this);
        RepaintPathGizmo();
    }

    public void SetMidBossPathPhase(int phaseIndex)
    {
        int clamped = Mathf.Clamp(phaseIndex, 0, StageTimelineBossPathEdit.MidBossPhaseCount - 1);
        if (previewMidBossPathPhase == clamped && pathEditTarget == E_StageTimelinePathEditTarget.MidBoss)
            return;

        previewMidBossPathPhase = clamped;
        pathEditTarget = E_StageTimelinePathEditTarget.MidBoss;
        EditorUtility.SetDirty(this);
        RepaintPathGizmo();
    }

    public void SetMainBossPathPhase(int phaseIndex)
    {
        int clamped = Mathf.Clamp(phaseIndex, 0, StageTimelineBossPathEdit.MainBossPhaseCount - 1);
        if (previewMainBossPathPhase == clamped && pathEditTarget == E_StageTimelinePathEditTarget.MainBoss)
            return;

        previewMainBossPathPhase = clamped;
        pathEditTarget = E_StageTimelinePathEditTarget.MainBoss;
        EditorUtility.SetDirty(this);
        RepaintPathGizmo();
    }

    public void SetMainBossSpellPhaseIndex(int phaseIndex)
    {
        int max = stageTimelineConfig?.mainBossEncounter?.bossPhases?.Count ?? 0;
        int clamped = max <= 0 ? 0 : Mathf.Clamp(phaseIndex, 0, max - 1);
        if (previewMainBossSpellPhaseIndex == clamped)
            return;

        previewMainBossSpellPhaseIndex = clamped;
        pathEditTarget = E_StageTimelinePathEditTarget.MainBoss;
        EditorUtility.SetDirty(this);
    }

    bool _embeddedEditPendingPreviewRestart;

    BattleStageBackgroundRuntime _backgroundPreviewRuntime;
    bool _backgroundPreviewBootstrapping;
    int _backgroundPreviewBootstrapGeneration;

    public bool IsBackgroundPreviewActive =>
        _backgroundPreviewRuntime != null && _backgroundPreviewRuntime.IsActive;

    public bool IsBackgroundPreviewBootstrapping => _backgroundPreviewBootstrapping;

    /// <summary>内嵌子配置（波次/Boss）在 Inspector 中修改后调用：刷新 Scene Gizmo，并标记运行时预览待重启。</summary>
    public void OnEmbeddedConfigChanged(UnityEngine.Object changedAsset = null)
    {
        if (stageTimelineConfig != null)
            EditorUtility.SetDirty(stageTimelineConfig);

        EmbeddedConfigChangedHook?.Invoke(this, changedAsset);

        RepaintPathGizmo();
        SceneView.RepaintAll();

        if (_previewActive)
            _embeddedEditPendingPreviewRestart = true;
    }

    /// <summary>Editor：Scene 外直接改子配置后同步嵌套 Inspector（由 Editor 程序集注册）。</summary>
    public static System.Action<StageTimelineConfigViewer, UnityEngine.Object> EmbeddedConfigChangedHook;

    public bool HasPendingPreviewRestart => _embeddedEditPendingPreviewRestart;

    public void RestartActivePreview()
    {
        if (!_previewActive && !_embeddedEditPendingPreviewRestart)
            return;

        var scope = _activePreviewScope;
        int previewIndex = previewMidStageWaveIndex;
        _embeddedEditPendingPreviewRestart = false;
        StopPreviewTimeline();

        if (!Application.isPlaying || !StageTimelinePreviewRuntime.CanPreview)
            return;

        switch (scope)
        {
            case E_StageTimelinePreviewScope.SingleMidStageWave:
                RequestPreviewMidStageWave(previewIndex);
                break;
            case E_StageTimelinePreviewScope.MidBossEncounter:
                RequestPreviewMidBoss();
                break;
            case E_StageTimelinePreviewScope.MainBossEncounter:
                RequestPreviewMainBoss();
                break;
            case E_StageTimelinePreviewScope.MainBossSpellPhase:
                RequestPreviewMainBossSpellPhase(previewMainBossSpellPhaseIndex);
                break;
            default:
                RequestPreviewStageTimeline();
                break;
        }
    }

    protected override void StopEditorPreviews()
    {
        StopBackgroundPreview();
        StopPreviewTimeline();
    }

    /// <summary>在 Scene 中播放当前关卡时间轴的背景滚动与云雾（需 Play 模式）。</summary>
    public void RequestBackgroundPreview()
    {
        if (_backgroundPreviewBootstrapping)
            return;

        if (stageTimelineConfig == null)
        {
            Logger.Warn("[StageTimelineTool] 未指定 StageTimelineConfig。", LogTag.Config);
            return;
        }

        if (!Application.isPlaying)
        {
            Logger.Warn("[StageTimelineTool] 请先进入 Play 模式后再预览背景。", LogTag.Config);
            return;
        }

        if (!StageTimelinePreviewRuntime.CanPreview)
        {
            Logger.Warn("[StageTimelineTool] 当前不可预览（非 Play 或战斗进行中）。", LogTag.Config);
            return;
        }

        var bgData = stageTimelineConfig.backgroundData;
        if (bgData == null || !bgData.enabled)
        {
            Logger.Warn("[StageTimelineTool] 背景未启用（backgroundData.enabled）。", LogTag.Config);
            return;
        }

        StopBackgroundPreview();
        _backgroundPreviewBootstrapping = true;
        int generation = ++_backgroundPreviewBootstrapGeneration;

        EditorApplication.delayCall += () =>
        {
            if (generation != _backgroundPreviewBootstrapGeneration || this == null)
                return;
            _ = BeginBackgroundPreviewAsync(generation);
        };
    }

    async Task BeginBackgroundPreviewAsync(int generation)
    {
        try
        {
            await StageTimelinePreviewRuntime.PrepareForPreviewAsync(battleAreaConfig).ConfigureAwait(true);

            if (generation != _backgroundPreviewBootstrapGeneration || this == null)
                return;

            _backgroundPreviewBootstrapping = false;
            StartBackgroundPreviewCore();
        }
        catch (System.Exception ex)
        {
            if (generation != _backgroundPreviewBootstrapGeneration || this == null)
                return;

            _backgroundPreviewBootstrapping = false;
            Logger.Warn($"[StageTimelineTool] 背景预览启动失败: {ex.Message}", LogTag.Config);
        }
    }

    void StartBackgroundPreviewCore()
    {
        if (!TryResolveGizmoBattleArea(out var area))
            return;

        var bgData = stageTimelineConfig.backgroundData;
        EnsureBackgroundPreviewRuntime();
        _backgroundPreviewRuntime.Apply(area, bgData, ResolveBackgroundPreviewSprite);

        if (!_backgroundPreviewRuntime.IsActive)
            Logger.Warn("[StageTimelineTool] 背景预览构建失败，请检查贴图 id 与对象池预热。", LogTag.Config);
    }

    /// <summary>停止背景 Scene 预览。</summary>
    public void StopBackgroundPreview()
    {
        _backgroundPreviewBootstrapGeneration++;
        _backgroundPreviewBootstrapping = false;

        if (_backgroundPreviewRuntime == null)
            return;

        _backgroundPreviewRuntime.DisposeInstance();
        _backgroundPreviewRuntime = null;
    }

    void EnsureBackgroundPreviewRuntime()
    {
        if (_backgroundPreviewRuntime != null)
            return;

        var go = new GameObject(BackgroundPreviewRuntimeName)
        {
            hideFlags = HideFlags.DontSave,
        };
        go.transform.SetParent(transform, false);
        _backgroundPreviewRuntime = go.AddComponent<BattleStageBackgroundRuntime>();
    }

    Sprite ResolveBackgroundPreviewSprite(string textureId)
    {
        if (string.IsNullOrEmpty(textureId) || !GameResDB.IsInitialized)
            return null;

        return GameResDB.Instance.GetSpriteFromTexture(textureId, 100f);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!enabled)
            return;

        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.update += OnEditorInspectorRefresh;
        RemoveLegacyAttachedBackgroundRuntime();
    }

    protected override void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.update -= OnEditorInspectorRefresh;
        _embeddedEditPendingPreviewRestart = false;
        base.OnDisable();
    }

    void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            StopEditorPreviews();
    }

    void RemoveLegacyAttachedBackgroundRuntime()
    {
        var legacy = GetComponent<BattleStageBackgroundRuntime>();
        if (legacy == null)
            return;

        if (Application.isPlaying)
            Destroy(legacy);
        else
            DestroyImmediate(legacy);

        EditorUtility.SetDirty(gameObject);
    }

    void OnEditorInspectorRefresh()
    {
        if (_previewBootstrapping
            || _backgroundPreviewBootstrapping
            || StageTimelinePreviewRuntime.IsLoading)
            RepaintActiveInspector();
    }

    static void RepaintActiveInspector()
    {
        var tracker = ActiveEditorTracker.sharedTracker;
        if (tracker?.activeEditors == null)
            return;

        for (int i = 0; i < tracker.activeEditors.Length; i++)
        {
            if (tracker.activeEditors[i]?.target is StageTimelineConfigViewer)
                tracker.activeEditors[i].Repaint();
        }
    }

    public void RequestPreviewStageTimeline()
        => RequestPreview(E_StageTimelinePreviewScope.FullTimeline, 0);

    public void RequestPreviewMidStageWave(int waveIndex)
        => RequestPreview(E_StageTimelinePreviewScope.SingleMidStageWave, waveIndex);

    public void RequestPreviewMidBoss()
        => RequestPreview(E_StageTimelinePreviewScope.MidBossEncounter, 0);

    public void RequestPreviewMainBoss()
        => RequestPreview(E_StageTimelinePreviewScope.MainBossEncounter, 0);

    public void RequestPreviewMainBossSpellPhase(int phaseIndex)
        => RequestPreview(E_StageTimelinePreviewScope.MainBossSpellPhase, phaseIndex);

    void RequestPreview(E_StageTimelinePreviewScope scope, int previewIndex)
    {
        if (_previewActive || _previewBootstrapping)
            return;

        if (stageTimelineConfig == null)
        {
            Logger.Warn("[StageTimelineConfigViewer] 未指定 StageTimelineConfig。", LogTag.Config);
            return;
        }

        if (!TryValidatePreviewTarget(scope, previewIndex, out string targetError))
        {
            Logger.Warn($"[StageTimelineConfigViewer] {targetError}", LogTag.Config);
            return;
        }

        if (!StageTimelinePreviewRuntime.CanPreview)
        {
            Logger.Warn("[StageTimelineConfigViewer] 当前不可预览（需 Play 模式且非战斗中）。", LogTag.Config);
            return;
        }

        StopPreviewTimeline();
        _previewBootstrapping = true;
        int generation = ++_bootstrapGeneration;
        var previewScope = scope;

        EditorApplication.delayCall += () =>
        {
            if (generation != _bootstrapGeneration || this == null)
                return;
            _ = BeginPreviewAsync(generation, previewScope, previewIndex);
        };
    }

    bool TryValidatePreviewTarget(E_StageTimelinePreviewScope scope, int previewIndex, out string error)
    {
        error = null;
        switch (scope)
        {
            case E_StageTimelinePreviewScope.SingleMidStageWave:
                if (stageTimelineConfig.midStageWaves == null || stageTimelineConfig.midStageWaves.Count == 0)
                {
                    error = "时间轴未配置道中波次。";
                    return false;
                }
                if (previewIndex < 0 || previewIndex >= stageTimelineConfig.midStageWaves.Count)
                {
                    error = $"波次索引 {previewIndex} 超出范围（0–{stageTimelineConfig.midStageWaves.Count - 1}）。";
                    return false;
                }
                if (stageTimelineConfig.midStageWaves[previewIndex] == null)
                {
                    error = $"道中波次 [{previewIndex}] 为空。";
                    return false;
                }
                return true;

            case E_StageTimelinePreviewScope.MidBossEncounter:
                var mid = stageTimelineConfig.midBossEncounter;
                if (mid == null || !mid.enabled || string.IsNullOrEmpty(mid.enemyConfigId))
                {
                    error = "未配置已启用的中场 Boss（midBossEncounter）。";
                    return false;
                }
                return true;

            case E_StageTimelinePreviewScope.MainBossEncounter:
                var main = stageTimelineConfig.mainBossEncounter;
                if (main == null || !main.enabled || string.IsNullOrEmpty(main.enemyConfigId))
                {
                    error = "未配置已启用的关底 Boss（mainBossEncounter）。";
                    return false;
                }
                return true;

            case E_StageTimelinePreviewScope.MainBossSpellPhase:
            {
                var encounter = stageTimelineConfig.mainBossEncounter;
                if (encounter == null || !encounter.enabled || string.IsNullOrEmpty(encounter.enemyConfigId))
                {
                    error = "未配置已启用的关底 Boss（mainBossEncounter）。";
                    return false;
                }
                if (encounter.bossPhases == null || encounter.bossPhases.Count == 0)
                {
                    error = "关底 Boss 未配置符卡阶段（bossPhases）。";
                    return false;
                }
                if (previewIndex < 0 || previewIndex >= encounter.bossPhases.Count || encounter.bossPhases[previewIndex] == null)
                {
                    error = $"符卡阶段索引 {previewIndex} 无效。";
                    return false;
                }
                return true;
            }

            default:
                return true;
        }
    }

    async Task BeginPreviewAsync(int generation, E_StageTimelinePreviewScope scope, int previewIndex)
    {
        try
        {
            await StageTimelinePreviewRuntime.PrepareForPreviewAsync(battleAreaConfig).ConfigureAwait(true);

            if (generation != _bootstrapGeneration || this == null)
                return;

            _previewBootstrapping = false;
            StartPreviewTimelineCore(scope, previewIndex);
        }
        catch (Exception ex)
        {
            if (generation != _bootstrapGeneration || this == null)
                return;

            _previewBootstrapping = false;
            Logger.Warn($"[StageTimelineConfigViewer] 预览启动失败: {ex.Message}", LogTag.Config);
        }
    }

    void StartPreviewTimelineCore(E_StageTimelinePreviewScope scope, int previewIndex)
    {
        BakeTimelineForPreview();

        _previewWorld = CreatePreviewWorld();
        _timelineSystem = _previewWorld.GetSystem<StageTimelineSystem>();
        _timelineSystem.Begin(stageTimelineConfig, scope, previewIndex);

        if (!_timelineSystem.IsActive)
        {
            ForceCleanupPreviewPresentation(_previewWorld);
            _previewWorld.Dispose();
            _previewWorld = null;
            _timelineSystem = null;
            StageTimelinePreviewRuntime.ReleasePreviewBattleAreaIfOwned();
            return;
        }

        _activePreviewScope = scope;
        if (scope == E_StageTimelinePreviewScope.SingleMidStageWave)
            previewMidStageWaveIndex = previewIndex;
        if (scope == E_StageTimelinePreviewScope.MainBossSpellPhase)
            previewMainBossSpellPhaseIndex = previewIndex;

        float duration = ResolvePreviewDurationSeconds(scope, previewIndex);
        uint fps = LogicFramePreviewClock.GetLogicFps();
        _previewClock = LogicFramePreviewClock.CreateRealTimeSession(duration, fps);
        _previewClock.Reset();
        _previewLogicFrame = 0;
        _previewActive = true;
        StageTimelinePreviewRuntime.ApplyTimelinePreviewAimAtPlayerFromFirstSpawn(battleAreaConfig);

        EditorApplication.update -= OnEditorPreviewUpdate;
        EditorApplication.update += OnEditorPreviewUpdate;

        Logger.Info(
            $"[StageTimelineConfigViewer] {DescribePreviewScope(scope, previewIndex)} 预览开始（{fps} FPS，约 {duration:F1}s）。",
            LogTag.Config);
    }

    public void StopPreviewTimeline()
    {
        _bootstrapGeneration++;
        _previewBootstrapping = false;

        if (!_previewActive && _previewWorld == null)
            return;

        _previewActive = false;
        _activePreviewScope = E_StageTimelinePreviewScope.FullTimeline;
        _embeddedEditPendingPreviewRestart = false;
        EditorApplication.update -= OnEditorPreviewUpdate;
        StageTimelinePreviewRuntime.ClearTimelinePreviewAimAtPlayer();

        _timelineSystem?.End();
        _timelineSystem = null;

        if (_previewWorld != null)
        {
            ForceCleanupPreviewPresentation(_previewWorld);
            _previewWorld.Dispose();
            _previewWorld = null;
        }

        StageTimelinePreviewRuntime.ReleasePreviewBattleAreaIfOwned();
        SceneView.RepaintAll();
    }

    float ResolvePreviewDurationSeconds(E_StageTimelinePreviewScope scope, int previewIndex)
    {
        if (previewDurationSeconds > 0f)
            return previewDurationSeconds;

        float estimated = EstimateScopedPreviewDurationSeconds(scope, previewIndex);
        if (estimated > 0f)
            return estimated;

        if (scope == E_StageTimelinePreviewScope.FullTimeline
            && stageTimelineConfig != null
            && stageTimelineConfig.maxStageDurationSeconds > 0f)
            return stageTimelineConfig.maxStageDurationSeconds;

        return scope switch
        {
            E_StageTimelinePreviewScope.SingleMidStageWave => 45f,
            E_StageTimelinePreviewScope.MidBossEncounter => 60f,
            E_StageTimelinePreviewScope.MainBossEncounter => 90f,
            E_StageTimelinePreviewScope.MainBossSpellPhase => 60f,
            _ => 60f,
        };
    }

    float EstimateScopedPreviewDurationSeconds(E_StageTimelinePreviewScope scope, int previewIndex)
    {
        if (stageTimelineConfig == null)
            return 0f;

        uint fps = LogicFramePreviewClock.GetLogicFps();
        float FrameToSec(int frames) => frames < 0 ? 0f : frames / (float)fps;

        switch (scope)
        {
            case E_StageTimelinePreviewScope.SingleMidStageWave:
            {
                var wave = stageTimelineConfig.midStageWaves[previewIndex];
                float motionSec = EstimateMovementDurationSeconds(wave?.pathRoute, fps);
                if (motionSec <= 0f && wave != null && wave.useDefaultDescentIfNoMovement)
                {
                    var area = StageTimelinePreviewRuntime.ResolveBattleAreaConfig(battleAreaConfig)?.battleAreaData
                               ?? BattleAreaData.Default;
                    float descent = Mathf.Max(0.01f, wave.defaultDescentSpeed);
                    motionSec = area.Height / descent + 2f;
                }
                return Mathf.Max(15f, motionSec + 8f);
            }

            case E_StageTimelinePreviewScope.MidBossEncounter:
            {
                var mid = stageTimelineConfig.midBossEncounter;
                float entry = EstimateMovementDurationSeconds(mid?.entryPathRoute, fps);
                float loop = EstimateMovementDurationSeconds(mid?.loopPathRoute, fps);
                float exit = EstimateMovementDurationSeconds(mid?.exitPathRoute, fps);
                float onField = mid != null ? mid.onFieldDurationSeconds : 0f;
                return Mathf.Max(30f, entry + onField + loop + exit + 10f);
            }

            case E_StageTimelinePreviewScope.MainBossEncounter:
            {
                var main = stageTimelineConfig.mainBossEncounter;
                if (main == null)
                    return 0f;

                float total = FrameToSec(main.bossIntroDurationFrames);
                if (main.bossPhases != null)
                {
                    for (int i = 0; i < main.bossPhases.Count; i++)
                    {
                        var phase = main.bossPhases[i];
                        if (phase == null || phase.triggerType != BossPhaseConfig.TriggerType.Time)
                            continue;
                        float phaseEnd = FrameToSec(phase.triggerFrameOffset);
                        if (phase.durationFrames >= 0)
                            phaseEnd += FrameToSec(phase.durationFrames);
                        else
                            phaseEnd += 30f;
                        total = Mathf.Max(total, phaseEnd);
                    }
                }
                return Mathf.Max(45f, total + 10f);
            }

            case E_StageTimelinePreviewScope.MainBossSpellPhase:
            {
                var main = stageTimelineConfig.mainBossEncounter;
                var phase = main.bossPhases[previewIndex];
                float duration = phase.durationSeconds >= 0f
                    ? phase.durationSeconds
                    : 30f;
                return Mathf.Max(20f, duration + 8f);
            }

            default:
                return 0f;
        }
    }

    static float EstimateMovementDurationSeconds(PathRouteMovementData pathRoute, uint fps) =>
        StageTimelineVisualSchedule.EstimateMovementDurationSeconds(pathRoute, fps);

    string DescribePreviewScope(E_StageTimelinePreviewScope scope, int previewIndex) => scope switch
    {
        E_StageTimelinePreviewScope.SingleMidStageWave => DescribeMidStageWavePreview(previewIndex),
        E_StageTimelinePreviewScope.MidBossEncounter => "中场 Boss",
        E_StageTimelinePreviewScope.MainBossEncounter => "关底 Boss",
        E_StageTimelinePreviewScope.MainBossSpellPhase => $"关底符卡 [{previewIndex}]",
        _ => "完整关卡时间轴",
    };

    string DescribeMidStageWavePreview(int waveIndex)
    {
        var waves = stageTimelineConfig?.midStageWaves;
        if (waves != null && waveIndex >= 0 && waveIndex < waves.Count && waves[waveIndex] != null)
            return EnemyWaveConfig.FormatTimelineLabel(waves[waveIndex], waveIndex);
        return $"道中波次 [{waveIndex}]";
    }

    public static string GetPreviewScopeDisplayName(E_StageTimelinePreviewScope scope) => scope switch
    {
        E_StageTimelinePreviewScope.SingleMidStageWave => "道中波次",
        E_StageTimelinePreviewScope.MidBossEncounter => "中场 Boss",
        E_StageTimelinePreviewScope.MainBossEncounter => "关底 Boss",
        E_StageTimelinePreviewScope.MainBossSpellPhase => "关底符卡阶段",
        _ => "完整时间轴",
    };

    void BakeTimelineForPreview()
    {
        uint fps = LogicFramePreviewClock.GetLogicFps();
        stageTimelineConfig.BakeLogicTiming(fps);
        EnemyPathBakeCache.Clear();

        if (stageTimelineConfig.midStageWaves == null)
            return;

        for (int i = 0; i < stageTimelineConfig.midStageWaves.Count; i++)
        {
            var wave = stageTimelineConfig.midStageWaves[i];
            if (wave is ILogicTimingBake bake)
                bake.BakeLogicTiming(fps);
            wave?.BakePathRouteIfNeeded(fps);
            wave?.ResolveDropReferences(GameResDB.Instance);
            wave?.ResolveSpawnQueueReferences(GameResDB.Instance);
        }

        if (stageTimelineConfig.midBossEncounter is ILogicTimingBake midBake)
            midBake.BakeLogicTiming(fps);
        stageTimelineConfig.midBossEncounter?.BakePathRoutesIfNeeded(fps);
        stageTimelineConfig.midBossEncounter?.ResolveReferences(GameResDB.Instance);
        if (stageTimelineConfig.mainBossEncounter is ILogicTimingBake mainBake)
            mainBake.BakeLogicTiming(fps);
        stageTimelineConfig.mainBossEncounter?.BakePathRoutesIfNeeded(fps);
        stageTimelineConfig.mainBossEncounter?.ResolveReferences(GameResDB.Instance);
    }

    static World CreatePreviewWorld()
    {
        var world = new World();
        world.AddSystem<StageTimelineSystem>();
        world.AddSystem<MidBossEncounterSystem>();
        world.AddSystem<MainBossEncounterSystem>();
        world.AddSystem<EnemyMovementSystem>();
        world.AddSystem<DanmakuSystem>();
        world.AddSystem<DanmakuEmitSystem>();
        world.AddSystem<PresentationSystem>();
        return world;
    }

    void OnEditorPreviewUpdate()
    {
        if (!_previewActive)
            return;

        if (this == null || !Application.isPlaying)
        {
            EditorApplication.update -= OnEditorPreviewUpdate;
            StopPreviewTimeline();
            return;
        }

        int steps = _previewClock.Tick(out bool stopped);
        for (int s = 0; s < steps; s++)
            StepPreviewLogicFrame();

        if (stopped)
            StopPreviewTimeline();
        else if (steps > 0)
            SceneView.RepaintAll();
    }

    void StepPreviewLogicFrame()
    {
        _previewWorld.LogicTick(_previewLogicFrame);
        _previewWorld.LateUpdate(_previewClock.LogicStepSeconds);
        _previewLogicFrame++;
    }

    static void ForceCleanupPreviewPresentation(World world)
    {
        var em = world.EntityManager;
        Span<int> links = em.GetActiveIndices<CGameObjectLink>();
        for (int i = 0; i < links.Length; i++)
        {
            int idx = links[i];
            Entity entity = em.GetEntity(idx);
            if (!em.IsValid(entity))
                continue;
            world.GameObjectBridge.Unlink(entity, em);
            em.DestroyEntity(entity);
        }

        Span<int> pendingSpawns = em.GetActiveIndices<CPoolGetTag>();
        for (int i = 0; i < pendingSpawns.Length; i++)
            em.DestroyEntity(em.GetEntity(pendingSpawns[i]));

        Span<int> recycle = em.GetActiveIndices<CPoolRecycleTag>();
        for (int i = 0; i < recycle.Length; i++)
            em.DestroyEntity(em.GetEntity(recycle[i]));
    }

    public uint PreviewLogicFrame => _previewLogicFrame;
    public float PreviewElapsedSeconds => _previewClock.ElapsedRealSeconds;

    public float ActivePreviewTotalDurationSeconds =>
        _previewActive ? _previewClock.MaxRealSeconds : 0f;

    void OnDrawGizmosSelected()
    {
        if (stageTimelineConfig == null)
            return;

        if (!TryResolveGizmoBattleArea(out var area))
            return;

        StageTimelineWaveGizmo.DrawBattleAreaFrames(area);

        uint fps = LogicFramePreviewClock.GetLogicFps();
        switch (pathEditTarget)
        {
            case E_StageTimelinePathEditTarget.MidBoss:
                DrawSelectedMidBossPathGizmo(area, fps);
                break;
            case E_StageTimelinePathEditTarget.MainBoss:
                DrawSelectedMainBossPathGizmo(area, fps);
                break;
            default:
                DrawSelectedWavePathGizmo(area, fps);
                break;
        }
    }

    void DrawSelectedMidBossPathGizmo(in BattleAreaData area, uint fps)
    {
        var mid = stageTimelineConfig.midBossEncounter;
        if (mid == null || !mid.enabled || string.IsNullOrEmpty(mid.enemyConfigId))
            return;

        var midVisuals = StageTimelineWaveGizmo.BuildMidBossEditorPathVisuals(mid, area, fps);
        StageTimelineWaveGizmo.DrawMidBossPathVisuals(midVisuals, mid, area, previewMidBossPathPhase);
    }

    void DrawSelectedMainBossPathGizmo(in BattleAreaData area, uint fps)
    {
        var main = stageTimelineConfig.mainBossEncounter;
        if (main == null || !main.enabled || string.IsNullOrEmpty(main.enemyConfigId))
            return;

        var mainVisuals = StageTimelineWaveGizmo.BuildMainBossEditorPathVisuals(main, area, fps);
        StageTimelineWaveGizmo.DrawMainBossPathVisuals(mainVisuals, main, area, previewMainBossPathPhase);
    }

    void DrawSelectedWavePathGizmo(in BattleAreaData area, uint fps)
    {
        int waveIndex = ResolveGizmoWaveIndex();
        if (waveIndex < 0)
            return;

        var waves = stageTimelineConfig.midStageWaves;
        if (waves == null || waveIndex >= waves.Count || waves[waveIndex] == null)
            return;

        var wave = waves[waveIndex];
        wave.EnsureSpawnQueueMigrated();
        int pathEntry = wave.ResolvePathDisplayEntryIndex(previewPathEditEntryIndex);
        StageTimelineWaveGizmo.DrawEditorWavePathPreview(wave, area, waveIndex, fps, pathEntry);
    }

    public void RepaintPathGizmo()
    {
        SceneView.RepaintAll();
    }

    /// <summary>当前 Scene 波次路径 Gizmo 的波次与战斗区（不要求路径可编辑）。</summary>
    public bool TryGetActiveWaveGizmoContext(out EnemyWaveConfig wave, out int waveIndex, out BattleAreaData area)
    {
        wave = null;
        waveIndex = -1;
        area = default;

        if (pathEditTarget != E_StageTimelinePathEditTarget.MidStageWave
            || stageTimelineConfig == null
            || !TryResolveGizmoBattleArea(out area))
            return false;

        waveIndex = ResolveGizmoWaveIndex();
        if (waveIndex < 0)
            return false;

        var waves = stageTimelineConfig.midStageWaves;
        if (waves == null || waveIndex >= waves.Count)
            return false;

        wave = waves[waveIndex];
        return wave != null;
    }

    /// <summary>当前 Scene 路径编辑/查看所对应的道中波次（与 Gizmo、内嵌编辑器波次索引一致）。</summary>
    public bool TryGetActiveWavePathEditContext(
        out EnemyWaveConfig wave,
        out int waveIndex,
        out int pathEditEntryIndex,
        out BattleAreaData area)
    {
        pathEditEntryIndex = 0;
        if (!TryGetActiveWaveGizmoContext(out wave, out waveIndex, out area))
            return false;

        wave.EnsureSpawnQueueMigrated();
        int entryCount = wave.ResolveSpawnCount();
        if (entryCount <= 0)
            return false;

        pathEditEntryIndex = wave.ResolvePathDisplayEntryIndex(previewPathEditEntryIndex);
        var route = wave.ResolveEditablePathRoute(pathEditEntryIndex);
        return PathRouteMovementData.HasAnyPathContent(route);
    }

    int ResolveGizmoWaveIndex()
    {
        if (_previewActive && _activePreviewScope == E_StageTimelinePreviewScope.SingleMidStageWave)
            return previewMidStageWaveIndex;

        if (stageTimelineConfig?.midStageWaves == null || stageTimelineConfig.midStageWaves.Count == 0)
            return -1;

        return Mathf.Clamp(previewMidStageWaveIndex, 0, stageTimelineConfig.midStageWaves.Count - 1);
    }

    public bool TryResolveGizmoBattleArea(out BattleAreaData area)
    {
        area = default;
        if (battleAreaConfig != null && battleAreaConfig.battleAreaData.Width > 0f)
        {
            area = battleAreaConfig.battleAreaData;
            return true;
        }

        var cfg = StageTimelinePreviewRuntime.ResolveBattleAreaConfig(battleAreaConfig);
        if (cfg != null)
        {
            area = cfg.battleAreaData;
            return area.Width > 0f && area.Height > 0f;
        }

        if (GlobalBattleData.IsInitialized)
        {
            area = GlobalBattleData.AreaData;
            return true;
        }

        return false;
    }
#endif
}

#if UNITY_EDITOR
/// <summary>Scene / Inspector 当前路径编辑对象。</summary>
public enum E_StageTimelinePathEditTarget : byte
{
    MidStageWave = 0,
    MidBoss = 1,
    MainBoss = 2,
}
#endif
