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

    [Header("配置文件")]
    public StageTimelineConfig stageTimelineConfig;

    [Header("战斗区（用于刷怪坐标与回收边界）")]
    [Tooltip("留空则在预览时从 GameResourceManifest.battleAreaConfigId 读取")]
    public BattleAreaConfig battleAreaConfig;

#if UNITY_EDITOR
    [Header("编辑器预览")]
    [Tooltip("预览时长（秒）；≤0 时按片段自动估算或使用关卡 maxStageDurationSeconds")]
    [SerializeField] float previewDurationSeconds = 120f;

    [SerializeField] int previewMidStageWaveIndex;

    [Header("Scene 可视化")]
    [Tooltip("在 Scene 视图绘制战斗区/回收区、刷怪点（黄）、运动路径（青）、退场点（品红）")]
    [SerializeField] bool drawWavePathGizmo = true;

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

    protected override void StopEditorPreviews() => StopPreviewTimeline();

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!enabled)
            return;

        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.update += OnEditorInspectorRefresh;
    }

    protected override void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.update -= OnEditorInspectorRefresh;
        base.OnDisable();
    }

    void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            StopPreviewTimeline();
    }

    void OnEditorInspectorRefresh()
    {
        if (_previewBootstrapping || StageTimelinePreviewRuntime.IsLoading)
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

    void RequestPreview(E_StageTimelinePreviewScope scope, int waveIndex)
    {
        if (_previewActive || _previewBootstrapping)
            return;

        if (stageTimelineConfig == null)
        {
            Logger.Warn("[StageTimelineConfigViewer] 未指定 StageTimelineConfig。", LogTag.Config);
            return;
        }

        if (!TryValidatePreviewTarget(scope, waveIndex, out string targetError))
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
        int previewWave = waveIndex;

        EditorApplication.delayCall += () =>
        {
            if (generation != _bootstrapGeneration || this == null)
                return;
            _ = BeginPreviewAsync(generation, previewScope, previewWave);
        };
    }

    bool TryValidatePreviewTarget(E_StageTimelinePreviewScope scope, int waveIndex, out string error)
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
                if (waveIndex < 0 || waveIndex >= stageTimelineConfig.midStageWaves.Count)
                {
                    error = $"波次索引 {waveIndex} 超出范围（0–{stageTimelineConfig.midStageWaves.Count - 1}）。";
                    return false;
                }
                if (stageTimelineConfig.midStageWaves[waveIndex] == null)
                {
                    error = $"道中波次 [{waveIndex}] 为空。";
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

            default:
                return true;
        }
    }

    async Task BeginPreviewAsync(int generation, E_StageTimelinePreviewScope scope, int waveIndex)
    {
        try
        {
            await StageTimelinePreviewRuntime.EnsureReadyAsync(battleAreaConfig).ConfigureAwait(true);

            if (generation != _bootstrapGeneration || this == null)
                return;

            _previewBootstrapping = false;

            if (!StageTimelinePreviewRuntime.TryValidateForPreview(battleAreaConfig, out string error))
            {
                Logger.Warn($"[StageTimelineConfigViewer] {error}", LogTag.Config);
                return;
            }

            if (!StageTimelinePreviewRuntime.TryApplyPreviewBattleArea(battleAreaConfig, out string areaError))
            {
                Logger.Warn($"[StageTimelineConfigViewer] {areaError}", LogTag.Config);
                return;
            }

            StartPreviewTimelineCore(scope, waveIndex);
        }
        catch (Exception ex)
        {
            if (generation != _bootstrapGeneration || this == null)
                return;

            _previewBootstrapping = false;
            Logger.Warn($"[StageTimelineConfigViewer] 预览启动失败: {ex.Message}", LogTag.Config);
        }
    }

    void StartPreviewTimelineCore(E_StageTimelinePreviewScope scope, int waveIndex)
    {
        BakeTimelineForPreview();

        _previewWorld = CreatePreviewWorld();
        _timelineSystem = _previewWorld.GetSystem<StageTimelineSystem>();
        _timelineSystem.Begin(stageTimelineConfig, scope, waveIndex);

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
            previewMidStageWaveIndex = waveIndex;

        float duration = ResolvePreviewDurationSeconds(scope, waveIndex);
        uint fps = LogicFramePreviewClock.GetLogicFps();
        _previewClock = LogicFramePreviewClock.CreateRealTimeSession(duration, fps);
        _previewClock.Reset();
        _previewLogicFrame = 0;
        _previewActive = true;

        EditorApplication.update -= OnEditorPreviewUpdate;
        EditorApplication.update += OnEditorPreviewUpdate;

        Logger.Info(
            $"[StageTimelineConfigViewer] {DescribePreviewScope(scope, waveIndex)} 预览开始（{fps} FPS，约 {duration:F1}s）。",
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
        EditorApplication.update -= OnEditorPreviewUpdate;

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

    float ResolvePreviewDurationSeconds(E_StageTimelinePreviewScope scope, int waveIndex)
    {
        if (previewDurationSeconds > 0f)
            return previewDurationSeconds;

        float estimated = EstimateScopedPreviewDurationSeconds(scope, waveIndex);
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
            _ => 60f,
        };
    }

    float EstimateScopedPreviewDurationSeconds(E_StageTimelinePreviewScope scope, int waveIndex)
    {
        if (stageTimelineConfig == null)
            return 0f;

        uint fps = LogicFramePreviewClock.GetLogicFps();
        float FrameToSec(int frames) => frames < 0 ? 0f : frames / (float)fps;

        switch (scope)
        {
            case E_StageTimelinePreviewScope.SingleMidStageWave:
            {
                var wave = stageTimelineConfig.midStageWaves[waveIndex];
                float motionSec = EstimateMovementDurationSeconds(wave?.movementData, fps);
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
                float intro = EstimateMovementDurationSeconds(mid?.introMovement, fps);
                return Mathf.Max(30f, intro + 15f);
            }

            case E_StageTimelinePreviewScope.MainBossEncounter:
            {
                var main = stageTimelineConfig.mainBossEncounter;
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

            default:
                return 0f;
        }
    }

    static float EstimateMovementDurationSeconds(MovementPatternData movement, uint fps)
    {
        if (movement == null)
            return 0f;
        if (movement.durationFrames < 0)
        {
            if (movement.durationSeconds < 0f)
                return 0f;
            return movement.durationSeconds;
        }
        return movement.durationFrames / (float)fps;
    }

    static string DescribePreviewScope(E_StageTimelinePreviewScope scope, int waveIndex) => scope switch
    {
        E_StageTimelinePreviewScope.SingleMidStageWave => $"道中波次 [{waveIndex}]",
        E_StageTimelinePreviewScope.MidBossEncounter => "中场 Boss",
        E_StageTimelinePreviewScope.MainBossEncounter => "关底 Boss",
        _ => "完整关卡时间轴",
    };

    public static string GetPreviewScopeDisplayName(E_StageTimelinePreviewScope scope) => scope switch
    {
        E_StageTimelinePreviewScope.SingleMidStageWave => "道中波次",
        E_StageTimelinePreviewScope.MidBossEncounter => "中场 Boss",
        E_StageTimelinePreviewScope.MainBossEncounter => "关底 Boss",
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
        }

        if (stageTimelineConfig.midBossEncounter is ILogicTimingBake midBake)
            midBake.BakeLogicTiming(fps);
        if (stageTimelineConfig.mainBossEncounter is ILogicTimingBake mainBake)
            mainBake.BakeLogicTiming(fps);
    }

    static World CreatePreviewWorld()
    {
        var world = new World();
        world.AddSystem<StageTimelineSystem>();
        world.AddSystem<EnemyMovementSystem>();
        world.AddSystem<DanmakuSystem>();
        world.AddSystem<DanmakuEmitSystem>();
        world.AddSystem<PresentationSystem>();
        world.AddSystem<PresentationPoseSystem>();
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

    void OnDrawGizmosSelected()
    {
        if (!drawWavePathGizmo || stageTimelineConfig == null)
            return;

        if (!TryResolveGizmoBattleArea(out var area))
            return;

        int waveIndex = ResolveGizmoWaveIndex();
        if (waveIndex < 0)
            return;

        var waves = stageTimelineConfig.midStageWaves;
        if (waves == null || waveIndex >= waves.Count || waves[waveIndex] == null)
            return;

        StageTimelineWaveGizmo.DrawBattleAreaFrames(area);
        var paths = StageTimelineWaveGizmo.BuildPathPreviews(
            waves[waveIndex], area, waveIndex, LogicFramePreviewClock.GetLogicFps());
        StageTimelineWaveGizmo.DrawPathPreviews(paths);
    }

    int ResolveGizmoWaveIndex()
    {
        if (_previewActive && _activePreviewScope == E_StageTimelinePreviewScope.SingleMidStageWave)
            return previewMidStageWaveIndex;

        if (stageTimelineConfig?.midStageWaves == null || stageTimelineConfig.midStageWaves.Count == 0)
            return -1;

        return Mathf.Clamp(previewMidStageWaveIndex, 0, stageTimelineConfig.midStageWaves.Count - 1);
    }

    bool TryResolveGizmoBattleArea(out BattleAreaData area)
    {
        area = default;
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
