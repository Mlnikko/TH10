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

    [Tooltip("Scene 路径编辑/高亮所对应的出怪队列条目索引")]
    [SerializeField] int previewPathEditEntryIndex;

    [Tooltip("当前 Scene 路径编辑对象")]
    [SerializeField] E_StageTimelinePathEditTarget pathEditTarget = E_StageTimelinePathEditTarget.MidStageWave;

    [Tooltip("中场 Boss 路径阶段：0=入场 1=循环 2=退场")]
    [SerializeField] int previewMidBossPathPhase;

    [Tooltip("关底 Boss 路径阶段：0=登场 1=场内")]
    [SerializeField] int previewMainBossPathPhase;

    [Tooltip("在 Scene 视图绘制战斗区、各路径节点与采样轨迹（无需进入 Play）")]
    [SerializeField] bool drawWavePathGizmo = true;

    [Tooltip("在 Scene 视图绘制中场 Boss 登场点与入场/循环/退场路径")]
    [SerializeField] bool drawMidBossPathGizmo = true;

    [Tooltip("在 Scene 视图绘制关底 Boss 登场点与路径")]
    [SerializeField] bool drawMainBossPathGizmo = true;

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
    public bool PathNodeSnapToGrid => pathNodeSnapToGrid;
    public float PathNodeSnapCellSize => pathNodeSnapCellSize;
    public bool DrawPathNodeSnapGrid => drawPathNodeSnapGrid;

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

    bool _embeddedEditPendingPreviewRestart;

    /// <summary>内嵌子配置（波次/Boss）在 Inspector 中修改后调用：刷新 Scene Gizmo，并标记运行时预览待重启。</summary>
    public void OnEmbeddedConfigChanged()
    {
        if (stageTimelineConfig != null)
            EditorUtility.SetDirty(stageTimelineConfig);

        RepaintPathGizmo();
        SceneView.RepaintAll();

        if (_previewActive)
            _embeddedEditPendingPreviewRestart = true;
    }

    public bool HasPendingPreviewRestart => _embeddedEditPendingPreviewRestart;

    public void RestartActivePreview()
    {
        if (!_previewActive && !_embeddedEditPendingPreviewRestart)
            return;

        var scope = _activePreviewScope;
        int waveIndex = previewMidStageWaveIndex;
        _embeddedEditPendingPreviewRestart = false;
        StopPreviewTimeline();

        if (!Application.isPlaying || !StageTimelinePreviewRuntime.CanPreview)
            return;

        switch (scope)
        {
            case E_StageTimelinePreviewScope.SingleMidStageWave:
                RequestPreviewMidStageWave(waveIndex);
                break;
            case E_StageTimelinePreviewScope.MidBossEncounter:
                RequestPreviewMidBoss();
                break;
            case E_StageTimelinePreviewScope.MainBossEncounter:
                RequestPreviewMainBoss();
                break;
            default:
                RequestPreviewStageTimeline();
                break;
        }
    }

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
        _embeddedEditPendingPreviewRestart = false;
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
        _embeddedEditPendingPreviewRestart = false;
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

    static float EstimateMovementDurationSeconds(PathRouteMovementData pathRoute, uint fps)
    {
        if (pathRoute == null)
            return 0f;

        pathRoute.BakeMovementTiming(fps);
        var baked = EnemyPathMovementBaking.BakeRoute(pathRoute, fps);
        if (baked.durationFrames > 0)
            return baked.durationFrames / (float)fps;
        if (pathRoute.durationSeconds >= 0f)
            return pathRoute.durationSeconds;
        return 0f;
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
        stageTimelineConfig.midBossEncounter?.BakePathRoutesIfNeeded(fps);
        stageTimelineConfig.midBossEncounter?.ResolveReferences(GameResDB.Instance);
        if (stageTimelineConfig.mainBossEncounter is ILogicTimingBake mainBake)
            mainBake.BakeLogicTiming(fps);
        stageTimelineConfig.mainBossEncounter?.BakePathRoutesIfNeeded(fps);
    }

    static World CreatePreviewWorld()
    {
        var world = new World();
        world.AddSystem<StageTimelineSystem>();
        world.AddSystem<MidBossEncounterSystem>();
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
        if (stageTimelineConfig == null)
            return;

        if (!TryResolveGizmoBattleArea(out var area))
            return;

        StageTimelineWaveGizmo.DrawBattleAreaFrames(area);

        uint fps = LogicFramePreviewClock.GetLogicFps();

        if (drawMidBossPathGizmo)
        {
            var mid = stageTimelineConfig.midBossEncounter;
            if (mid != null && mid.enabled && !string.IsNullOrEmpty(mid.enemyConfigId))
            {
                int emph = pathEditTarget == E_StageTimelinePathEditTarget.MidBoss
                    ? previewMidBossPathPhase
                    : -1;
                var midVisuals = StageTimelineWaveGizmo.BuildMidBossEditorPathVisuals(mid, area, fps);
                StageTimelineWaveGizmo.DrawMidBossPathVisuals(midVisuals, mid, area, emph);
            }
        }

        if (drawMainBossPathGizmo)
        {
            var main = stageTimelineConfig.mainBossEncounter;
            if (main != null && main.enabled && !string.IsNullOrEmpty(main.enemyConfigId))
            {
                int emph = pathEditTarget == E_StageTimelinePathEditTarget.MainBoss
                    ? previewMainBossPathPhase
                    : -1;
                var mainVisuals = StageTimelineWaveGizmo.BuildMainBossEditorPathVisuals(main, area, fps);
                StageTimelineWaveGizmo.DrawMainBossPathVisuals(mainVisuals, main, area, emph);
            }
        }

        if (!drawWavePathGizmo)
            return;

        int waveIndex = ResolveGizmoWaveIndex();
        if (waveIndex < 0)
            return;

        var waves = stageTimelineConfig.midStageWaves;
        if (waves == null || waveIndex >= waves.Count || waves[waveIndex] == null)
            return;

        var wave = waves[waveIndex];
        wave.EnsureSpawnQueueMigrated();
        int pathEntry = wave.ResolvePathDisplayEntryIndex(previewPathEditEntryIndex);
        StageTimelineWaveGizmo.DrawEditorWavePathPreview(
            wave,
            area,
            waveIndex,
            LogicFramePreviewClock.GetLogicFps(),
            pathEntry);
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

        if (!drawWavePathGizmo || stageTimelineConfig == null || !TryResolveGizmoBattleArea(out area))
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
        if (wave.UsesPerQueueEntryPaths)
            wave.EnsureEntryPathOverrideInitialized(pathEditEntryIndex);

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
