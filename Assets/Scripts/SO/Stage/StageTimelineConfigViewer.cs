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
    protected override bool HasAssignedConfig => stageTimelineConfig != null;

    [Header("配置文件")]
    public StageTimelineConfig stageTimelineConfig;

    [Header("战斗区（用于刷怪坐标与回收边界）")]
    [Tooltip("留空则在预览时从 GameResourceManifest.battleAreaConfigId 读取")]
    public BattleAreaConfig battleAreaConfig;

#if UNITY_EDITOR
    [Header("编辑器预览")]
    [Tooltip("预览时长（秒）；≤0 时使用 StageTimelineConfig.maxStageDurationSeconds")]
    [SerializeField] float previewDurationSeconds = 120f;

    bool _previewActive;
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

    protected override void StopEditorPreviews() => StopPreviewTimeline();

    void OnEnable()
    {
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
    {
        if (_previewActive || _previewBootstrapping)
            return;

        if (stageTimelineConfig == null)
        {
            Logger.Warn("[StageTimelineConfigViewer] 未指定 StageTimelineConfig。", LogTag.Config);
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

        EditorApplication.delayCall += () =>
        {
            if (generation != _bootstrapGeneration || this == null)
                return;
            _ = BeginPreviewAsync(generation);
        };
    }

    async Task BeginPreviewAsync(int generation)
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

            StartPreviewTimelineCore();
        }
        catch (Exception ex)
        {
            if (generation != _bootstrapGeneration || this == null)
                return;

            _previewBootstrapping = false;
            Logger.Warn($"[StageTimelineConfigViewer] 预览启动失败: {ex.Message}", LogTag.Config);
        }
    }

    void StartPreviewTimelineCore()
    {
        BakeTimelineForPreview();

        _previewWorld = CreatePreviewWorld();
        _timelineSystem = _previewWorld.GetSystem<StageTimelineSystem>();
        _timelineSystem.Begin(stageTimelineConfig);

        float duration = ResolvePreviewDurationSeconds();
        uint fps = LogicFramePreviewClock.GetLogicFps();
        _previewClock = LogicFramePreviewClock.CreateRealTimeSession(duration, fps);
        _previewClock.Reset();
        _previewLogicFrame = 0;
        _previewActive = true;

        EditorApplication.update -= OnEditorPreviewUpdate;
        EditorApplication.update += OnEditorPreviewUpdate;

        Logger.Info($"[StageTimelineConfigViewer] 预览开始（{fps} FPS，约 {duration:F1}s）。", LogTag.Config);
    }

    public void StopPreviewTimeline()
    {
        _bootstrapGeneration++;
        _previewBootstrapping = false;

        if (!_previewActive && _previewWorld == null)
            return;

        _previewActive = false;
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

    float ResolvePreviewDurationSeconds()
    {
        if (previewDurationSeconds > 0f)
            return previewDurationSeconds;
        if (stageTimelineConfig != null && stageTimelineConfig.maxStageDurationSeconds > 0f)
            return stageTimelineConfig.maxStageDurationSeconds;
        return 60f;
    }

    void BakeTimelineForPreview()
    {
        uint fps = LogicFramePreviewClock.GetLogicFps();
        stageTimelineConfig.BakeLogicTiming(fps);

        if (stageTimelineConfig.midStageWaves == null)
            return;

        for (int i = 0; i < stageTimelineConfig.midStageWaves.Count; i++)
        {
            var wave = stageTimelineConfig.midStageWaves[i];
            if (wave is ILogicTimingBake bake)
                bake.BakeLogicTiming(fps);
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
#endif
}
