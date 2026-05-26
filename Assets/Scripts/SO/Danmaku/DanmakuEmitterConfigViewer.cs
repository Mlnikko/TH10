using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>发射器预制体配置编辑；不参与运行时逻辑。</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DanmakuEmitterConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => emitterConfig != null;

    [Header("配置文件")]
    public DanmakuEmitterConfig emitterConfig;

    [SerializeField] EmitMode emitterType;

    [Header("发射器阵营")]
    [SerializeField] EmitterCamp emitterCamp;

    [Header("发射器显示")]
    [SerializeField] Sprite displaySprite;

    [Tooltip("发射器 Sprite 自转（度/秒，>0 时持续自转；编辑器内用「预览自转」查看）")]
    [SerializeField] float displaySelfSpinDegreesPerSecond;

    [Header("循环缩放（相对预制体倍数）")]
    [SerializeField, Min(0.01f)] float displayScaleMin = 1f;
    [SerializeField, Min(0.01f)] float displayScaleMax = 1f;
    [Tooltip("缩放循环频率（次/秒）；0 表示不循环缩放")]
    [SerializeField, Min(0f)] float displayScaleCyclesPerSecond;

    [Header("发射器Offset调整")]
    [SerializeField] Vector2 emitPosOffset;
    [SerializeField] float emitRotOffsetZ;

    [Header("装填弹幕旋转")]
    [SerializeField] float danmakuRotOffsetZ;

    [Header("Line Mode 参数")]
    [SerializeField] LineModeConfig lineModeConfig;

    [Header("Arc Mode 参数")]
    [SerializeField] ArcModeConfig arcModeConfig;

    [Header("Wave Mode 参数（波弹）")]
    [SerializeField] WaveModeConfig waveModeConfig;

    [Header("Grain Mode 参数（粒弹）")]
    [SerializeField] GrainModeConfig grainModeConfig;

    [Header("发射音效")]
    [SerializeField] AudioName launchAudio;

    [Header("发射间隔（秒）")]
    [SerializeField] float launchIntervalSeconds;

    [Header("发射次数")]
    [Tooltip("-1 表示无限次数")]
    [SerializeField] int launchCount = -1;

    [Header("发射速度")]
    [SerializeField] float launchSpeed;

#if UNITY_EDITOR
    [Header("编辑器预览")]
    [Tooltip("预览模拟时长（秒），按 GameManager.logicFPS 换算为逻辑帧")]
    [SerializeField] float previewDuration = 5f;

    [Tooltip("预览弹幕存活时间（秒），按逻辑帧计数后回收")]
    [SerializeField] float previewBulletLifetime = 3f;

    [Tooltip("自转预览时长（秒），到时自动停止并复位")]
    [SerializeField] float previewSpinDuration = 5f;

    [SerializeField] bool drawPreviewSpawnGizmos = true;

    bool _previewActive;
    bool _spinPreviewActive;
    float _spinPreviewElapsed;
    LogicFramePreviewRunner _previewClock;
    int _previewBulletLifetimeFrames;
    uint _previewLastFireFrame;
    int _previewLaunchCountUsed;
    CDanmakuEmitter _previewEmitter;
    int _previewEmitParamHash;
    int _previewSequentialBulletIndex;
    readonly List<DanmakuEmitterSpawnMath.SpawnSample> _lastBurstSamples = new();
    readonly List<PreviewBullet> _previewBullets = new();
    GameObject _previewRoot;
    float _editorDisplaySpinRad;
    float _editorDisplayScalePhaseRad;
    Quaternion _emitBaseLocalRotation = Quaternion.identity;
    Vector3 _emitBaseLocalScale = Vector3.one;

    struct PreviewBullet
    {
        public GameObject go;
        public DanmakuConfig danmaku;
        public float velX;
        public float velY;
        public int ageFrames;
    }
#endif

    public bool LoadEmitterConfig()
    {
        if (!HasAssignedConfig)
        {
            Logger.Warn("发射器配置为空，无法加载", LogTag.Config);
            return false;
        }

        LoadFromConfig();
        return true;
    }

    public override void LoadFromConfig()
    {
        if (emitterConfig == null)
            return;

        emitterType = emitterConfig.emitMode;
        emitterCamp = emitterConfig.emitterCamp;
        displaySprite = emitterConfig.displaySprite;
        displaySelfSpinDegreesPerSecond = emitterConfig.displaySelfSpinDegreesPerSecond;
        displayScaleMin = emitterConfig.displayScaleMin;
        displayScaleMax = emitterConfig.displayScaleMax;
        displayScaleCyclesPerSecond = emitterConfig.displayScaleCyclesPerSecond;
        emitPosOffset = emitterConfig.emitterPosOffset;
        emitRotOffsetZ = emitterConfig.emitterRotOffsetZ;
        danmakuRotOffsetZ = emitterConfig.danmakuRotOffsetZ;
        lineModeConfig = emitterConfig.lineModeConfig;
        arcModeConfig = emitterConfig.arcModeConfig;
        waveModeConfig = emitterConfig.waveModeConfig;
        grainModeConfig = emitterConfig.grainModeConfig;
        launchIntervalSeconds = emitterConfig.launchIntervalSeconds;
        launchCount = DanmakuEmitterSalvoInfo.NormalizeLaunchCountMax(emitterConfig.launchCount);
        launchSpeed = emitterConfig.launchSpeed;
        launchAudio = emitterConfig.audio_Fire;

        ApplyEditorPreview();
        Logger.Debug("已加载发射器配置" + emitterConfig.name, LogTag.Config);
    }

    protected override void ApplyEditorPreview()
    {
        SyncDisplaySpriteFromConfig();
#if UNITY_EDITOR
        if (emitterConfig != null)
            ConfigViewerPrefabSync.ApplyDanmakuEmitterDisplaySprite(emitterConfig);

        if (!_previewActive && !_spinPreviewActive)
            CaptureEmitBaseLocalRotation();
        else if (_previewActive)
            RefreshEmitterPreviewLive();
#endif
    }

    /// <summary>将当前绑定 Config 的 displaySprite 写入 SpriteRenderer（编辑器用）。</summary>
    public void SyncDisplaySpriteFromConfig()
    {
        Sprite sprite = emitterConfig != null ? emitterConfig.displaySprite : displaySprite;
        DanmakuEmitterPresentation.Apply(sprite, gameObject);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        EditorApplication.delayCall += () =>
        {
            if (this != null)
                SyncDisplaySpriteFromConfig();
        };
    }

    void CaptureEmitBaseLocalRotation()
    {
        _emitBaseLocalRotation = transform.localRotation;
        _emitBaseLocalScale = transform.localScale;
        _editorDisplaySpinRad = 0f;
        _editorDisplayScalePhaseRad = 0f;
    }

    void ResetPreviewDisplayRotation()
    {
        _editorDisplaySpinRad = 0f;
        _editorDisplayScalePhaseRad = 0f;
        transform.localRotation = _emitBaseLocalRotation;
        transform.localScale = _emitBaseLocalScale;
    }

    bool HasDisplayScalePulse() =>
        DanmakuEmitterDisplaySpin.HasScalePulse(
            displayScaleMin,
            displayScaleMax,
            displayScaleCyclesPerSecond);

    float GetEmitBaseRotZDeg() =>
        (_previewActive || _spinPreviewActive)
            ? _emitBaseLocalRotation.eulerAngles.z
            : transform.eulerAngles.z;
#endif

    public void SaveEmitterConfig()
    {
        if (emitterConfig == null)
        {
            Logger.Warn("发射器配置为空，无法保存", LogTag.Config);
            return;
        }

        SyncViewerFieldsToConfig();
        Logger.Debug("成功保存发射器配置" + emitterConfig.name, LogTag.Config);
    }

    void SyncViewerFieldsToConfig()
    {
        if (emitterConfig == null)
            return;

        emitterConfig.emitMode = emitterType;
        emitterConfig.emitterCamp = emitterCamp;
        emitterConfig.displaySprite = displaySprite;
        emitterConfig.displaySelfSpinDegreesPerSecond = displaySelfSpinDegreesPerSecond;
        emitterConfig.displayScaleMin = displayScaleMin;
        emitterConfig.displayScaleMax = displayScaleMax;
        emitterConfig.displayScaleCyclesPerSecond = displayScaleCyclesPerSecond;
        emitterConfig.emitterPosOffset = emitPosOffset;
        emitterConfig.emitterRotOffsetZ = emitRotOffsetZ;
        emitterConfig.danmakuRotOffsetZ = danmakuRotOffsetZ;
        emitterConfig.lineModeConfig = lineModeConfig;
        emitterConfig.arcModeConfig = arcModeConfig;
        emitterConfig.waveModeConfig = waveModeConfig;
        emitterConfig.grainModeConfig = grainModeConfig;
        emitterConfig.launchIntervalSeconds = launchIntervalSeconds;
        launchCount = DanmakuEmitterSalvoInfo.NormalizeLaunchCountMax(launchCount);
        emitterConfig.launchCount = launchCount;
        emitterConfig.launchSpeed = launchSpeed;
        emitterConfig.audio_Fire = launchAudio;

        uint logicFps = GameManager.logicFPS > 0 ? GameManager.logicFPS : 60u;
        emitterConfig.BakeLogicTiming(logicFps);

#if UNITY_EDITOR
        ConfigViewerPrefabSync.ApplyDanmakuEmitterDisplaySprite(emitterConfig);
        SyncDisplaySpriteFromConfig();
#endif
    }

    public void PreviewEmitterEffect()
    {
#if UNITY_EDITOR
        StartPreviewEmitter();
#else
        LoadEmitterConfig();
#endif
    }

#if UNITY_EDITOR
    public bool IsPreviewingEmitter => _previewActive;

    public bool IsPreviewingDisplaySpin => _spinPreviewActive;

    protected override void StopEditorPreviews()
    {
        StopPreviewEmitter();
        StopPreviewDisplaySpin();
    }

    public void StartPreviewDisplaySpin()
    {
        StopPreviewDisplaySpin();
        StopPreviewEmitter();

        if (Mathf.Approximately(displaySelfSpinDegreesPerSecond, 0f) && !HasDisplayScalePulse())
        {
            Logger.Warn(
                "[DanmakuEmitterConfigViewer] 自转速度为 0 且未配置循环缩放，请调大 displaySelfSpinDegreesPerSecond 或设置缩放区间与频率。",
                LogTag.Config);
            return;
        }

        CaptureEmitBaseLocalRotation();
        _spinPreviewActive = true;
        _spinPreviewElapsed = 0f;

        EditorApplication.update -= OnEditorSpinPreviewUpdate;
        EditorApplication.update += OnEditorSpinPreviewUpdate;

        Logger.Info($"[DanmakuEmitterConfigViewer] 自转预览开始（{previewSpinDuration:F1}s）。", LogTag.Config);
    }

    public void StopPreviewDisplaySpin()
    {
        if (!_spinPreviewActive)
            return;

        _spinPreviewActive = false;
        EditorApplication.update -= OnEditorSpinPreviewUpdate;
        ResetPreviewDisplayRotation();
        SceneView.RepaintAll();
    }

    void OnEditorSpinPreviewUpdate()
    {
        if (!_spinPreviewActive)
            return;

        if (this == null)
        {
            EditorApplication.update -= OnEditorSpinPreviewUpdate;
            return;
        }

        _spinPreviewElapsed += Time.deltaTime;
        if (_spinPreviewElapsed >= previewSpinDuration)
        {
            StopPreviewDisplaySpin();
            return;
        }

        _editorDisplaySpinRad += displaySelfSpinDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime;
        transform.localRotation = _emitBaseLocalRotation * Quaternion.Euler(0f, 0f, _editorDisplaySpinRad * Mathf.Rad2Deg);

        if (HasDisplayScalePulse())
        {
            _editorDisplayScalePhaseRad += displayScaleCyclesPerSecond * Mathf.PI * 2f * Time.deltaTime;
            transform.localScale = DanmakuEmitterDisplaySpin.GetLocalScale(
                _emitBaseLocalScale,
                displayScaleMin,
                displayScaleMax,
                _editorDisplayScalePhaseRad,
                displayScaleCyclesPerSecond);
        }

        SceneView.RepaintAll();
    }

    public void StartPreviewEmitter()
    {
        StopPreviewEmitter();
        StopPreviewDisplaySpin();

        if (emitterConfig == null)
        {
            Logger.Warn("[DanmakuEmitterConfigViewer] 未指定 DanmakuEmitterConfig，无法预览。", LogTag.Config);
            return;
        }

        if (emitterType == EmitMode.None)
        {
            Logger.Warn("[DanmakuEmitterConfigViewer] 发射模式为 None，请改为 Line / Arc / Wave / Grain。", LogTag.Config);
            return;
        }

        if (launchCount == 0)
        {
            launchCount = -1;
            Logger.Warn(
                "[DanmakuEmitterConfigViewer] launchCount 为 0 已按无限齐射处理；请在 SO 中设为 -1 或正整数。",
                LogTag.Config);
        }

        var previewEmitterProbe = BuildEmitterFromViewerFields();
        if (DanmakuEmitterSalvoInfo.GetSalvoBulletCount(in previewEmitterProbe) <= 0)
        {
            Logger.Warn(
                "[DanmakuEmitterConfigViewer] 当前模式每齐射弹数为 0，无法预览；请检查 lineCount / arcBulletCount / bulletCount。",
                LogTag.Config);
            return;
        }

        if (launchSpeed <= 0f)
        {
            Logger.Warn("[DanmakuEmitterConfigViewer] launchSpeed 为 0，弹幕会生成但无法移动，请调大发射速度。", LogTag.Config);
        }

        var danmaku = ResolvePreviewDanmakuConfig();
        if (danmaku == null || danmaku.sprite == null)
        {
            Logger.Warn(
                "[DanmakuEmitterConfigViewer] 未找到预览用 DanmakuConfig 或 Sprite，请检查 emitterConfig.danmakuConfigIds。",
                LogTag.Config);
            return;
        }

        uint fps = LogicFramePreviewClock.GetLogicFps();
        _previewClock = LogicFramePreviewClock.CreateLogicFrameSession(previewDuration, fps);
        _previewClock.Reset();
        _previewLastFireFrame = 0;
        _previewLaunchCountUsed = 0;
        _previewSequentialBulletIndex = 0;
        _previewBulletLifetimeFrames = LogicFramePreviewClock.SecondsToLogicFrames(previewBulletLifetime, fps);
        _previewEmitter = BuildEmitterFromViewerFields();
        _previewEmitParamHash = ComputeEmitPreviewParameterHash();
        _previewActive = true;
        CaptureEmitBaseLocalRotation();
        RefreshPreviewGizmoSamples();

        EditorApplication.update -= OnEditorPreviewUpdate;
        EditorApplication.update += OnEditorPreviewUpdate;

        Logger.Info(
            $"[DanmakuEmitterConfigViewer] 发射预览开始（{fps} 逻辑FPS，" +
            $"约 {_previewClock.MaxLogicFrames} 帧，发射冷却 {_previewEmitter.launchCooldownFrames} 逻辑帧）。",
            LogTag.Config);
    }

    public void StopPreviewEmitter()
    {
        if (!_previewActive && _previewBullets.Count == 0 && _lastBurstSamples.Count == 0)
            return;

        _previewActive = false;
        EditorApplication.update -= OnEditorPreviewUpdate;

        ClearPreviewBullets();
        ConfigViewerEditorScene.DestroyRoot(ref _previewRoot);
        _lastBurstSamples.Clear();

        SceneView.RepaintAll();
    }

    /// <summary>
    /// 发射预览进行中时，将当前 Viewer / SO 字段同步到预览状态（无需停止再开）。
    /// </summary>
    public void RefreshEmitterPreviewLive()
    {
        if (!_previewActive)
            return;

        int newHash = ComputeEmitPreviewParameterHash();
        if (newHash != _previewEmitParamHash)
        {
            _previewEmitParamHash = newHash;
            ClearPreviewBullets();
        }

        _previewEmitter = BuildEmitterFromViewerFields();
        uint fps = LogicFramePreviewClock.GetLogicFps();
        _previewBulletLifetimeFrames =
            LogicFramePreviewClock.SecondsToLogicFrames(previewBulletLifetime, fps);

        SyncDisplaySpriteFromConfig();
        RefreshPreviewGizmoSamples();
        SceneView.RepaintAll();
    }

    int ComputeEmitPreviewParameterHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (int)emitterType;
            hash = hash * 31 + (int)emitterCamp;
            hash = hash * 31 + (emitterConfig?.aimAtPlayer == true ? 1 : 0);
            hash = hash * 31 + emitPosOffset.GetHashCode();
            hash = hash * 31 + emitRotOffsetZ.GetHashCode();
            hash = hash * 31 + danmakuRotOffsetZ.GetHashCode();
            hash = hash * 31 + launchIntervalSeconds.GetHashCode();
            hash = hash * 31 + launchCount;
            hash = hash * 31 + launchSpeed.GetHashCode();
            hash = hash * 31 + lineModeConfig.GetHashCode();
            hash = hash * 31 + arcModeConfig.GetHashCode();
            hash = hash * 31 + waveModeConfig.GetHashCode();
            hash = hash * 31 + grainModeConfig.GetHashCode();

            if (emitterConfig != null)
            {
                hash = hash * 31 + (int)emitterConfig.danmakuSelectMode;
                hash = hash * 31 + emitterConfig.salvoAngleAdvanceDeg.GetHashCode();
                hash = hash * 31 + HashDanmakuConfigIds(emitterConfig.danmakuConfigIds);
            }

            return hash;
        }
    }

    static int HashDanmakuConfigIds(string[] ids)
    {
        if (ids == null || ids.Length == 0)
            return 0;

        int hash = 17;
        for (int i = 0; i < ids.Length; i++)
            hash = hash * 31 + (ids[i]?.GetHashCode() ?? 0);
        return hash;
    }

    void RefreshPreviewGizmoSamples()
    {
        _lastBurstSamples.Clear();
        if (emitterType == EmitMode.None)
            return;

        Vector3 origin = transform.position;
        float emitRotRad = GetPreviewEmitRotRad();
        uint logicFrame = _previewActive ? _previewClock.LogicFrame : 0;
        DanmakuEmitterSpawnMath.CollectSpawns(
            in _previewEmitter,
            origin.x,
            origin.y,
            emitRotRad,
            _lastBurstSamples,
            logicFrame,
            _previewLaunchCountUsed);
    }

    void OnEditorPreviewUpdate()
    {
        if (!_previewActive)
            return;

        if (this == null)
        {
            EditorApplication.update -= OnEditorPreviewUpdate;
            return;
        }

        int steps = _previewClock.Tick(out bool stopped);

        for (int s = 0; s < steps; s++)
            StepPreviewLogicFrame();

        if (stopped)
            StopPreviewEmitter();
        else if (steps > 0)
            SceneView.RepaintAll();
    }

    void StepPreviewLogicFrame()
    {
        AdvancePreviewBulletsPerLogicFrame();
        TryFirePreviewOnLogicFrame();
    }

    void TryFirePreviewOnLogicFrame()
    {
        _previewEmitter = BuildEmitterFromViewerFields();

        if (launchCount > 0 && _previewLaunchCountUsed >= launchCount)
            return;

        uint framesSinceLastFire = _previewClock.LogicFrame - _previewLastFireFrame;
        if (_previewEmitter.launchCooldownFrames > 0 &&
            framesSinceLastFire < (uint)_previewEmitter.launchCooldownFrames)
        {
            return;
        }

        var danmaku = ResolvePreviewDanmakuConfig();
        if (danmaku == null)
            return;

        FirePreviewBurst(danmaku);
        _previewLastFireFrame = _previewClock.LogicFrame;
        _previewLaunchCountUsed++;
    }

    void FirePreviewBurst(DanmakuConfig danmaku)
    {
        Vector3 origin = transform.position;
        float emitRotRad = GetPreviewEmitRotRad();

        _lastBurstSamples.Clear();
        DanmakuEmitterSpawnMath.CollectSpawns(
            in _previewEmitter,
            origin.x,
            origin.y,
            emitRotRad,
            _lastBurstSamples,
            _previewClock.LogicFrame,
            _previewLaunchCountUsed);

        if (_lastBurstSamples.Count == 0)
        {
            int salvo = DanmakuEmitterSalvoInfo.GetSalvoBulletCount(in _previewEmitter);
            Logger.Warn(
                $"[DanmakuEmitterConfigViewer] 本次齐射 0 发弹幕（模式 {_previewEmitter.emitMode}，配置弹数 {salvo}）。",
                LogTag.Config);
            return;
        }

        for (int i = 0; i < _lastBurstSamples.Count; i++)
        {
            var s = _lastBurstSamples[i];
            SpawnPreviewBullet(danmaku, s.posX, s.posY, s.rotRad, s.velX, s.velY);
        }

        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    void SpawnPreviewBullet(DanmakuConfig danmaku, float x, float y, float rotRad, float velX, float velY)
    {
        var go = new GameObject("PreviewDanmaku");
        if (!ConfigViewerEditorScene.AttachTransientObject(go, transform, ref _previewRoot, $"{name}_EmitterPreview"))
            return;

        go.AddComponent<SpriteRenderer>();
        ConfigViewerSpritePreview.ApplySortingFrom(go.transform, transform);
        DanmakuPresentation.Apply(danmaku, go);
        go.transform.position = new Vector3(x, y, transform.position.z);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotRad * Mathf.Rad2Deg);

        _previewBullets.Add(new PreviewBullet
        {
            go = go,
            danmaku = danmaku,
            velX = velX,
            velY = velY,
            ageFrames = 0,
        });
    }

    void AdvancePreviewBulletsPerLogicFrame()
    {
        for (int i = _previewBullets.Count - 1; i >= 0; i--)
        {
            var b = _previewBullets[i];
            if (b.go == null)
            {
                _previewBullets.RemoveAt(i);
                continue;
            }

            Vector3 pos = b.go.transform.position;

            b.ageFrames++;
            if (_previewBulletLifetimeFrames > 0 && b.ageFrames >= _previewBulletLifetimeFrames)
            {
                DanmakuHitEffectPresentation.TrySpawnAtConfig(b.danmaku, pos.x, pos.y);
                DestroyImmediate(b.go);
                _previewBullets.RemoveAt(i);
                continue;
            }

            pos.x += b.velX;
            pos.y += b.velY;
            b.go.transform.position = pos;
            _previewBullets[i] = b;
        }
    }

    void ClearPreviewBullets()
    {
        for (int i = 0; i < _previewBullets.Count; i++)
        {
            if (_previewBullets[i].go != null)
                DestroyImmediate(_previewBullets[i].go);
        }

        _previewBullets.Clear();
    }

    float GetPreviewEmitRotRad(in CDanmakuEmitter emitter)
    {
        float baseRad = GetEmitBaseRotZDeg() * Mathf.Deg2Rad;
        Vector3 origin = transform.position;
        float? previewTargetY = emitter.aimAtPlayer ? origin.y - 3f : null;
        float? previewTargetX = previewTargetY.HasValue ? origin.x : null;

        return DanmakuEmitterAimAtPlayerLogic.ResolveEmitRotRad(
            in emitter,
            origin.x,
            origin.y,
            baseRad,
            em: null,
            overrideTargetX: previewTargetX,
            overrideTargetY: previewTargetY);
    }

    float GetPreviewEmitRotRad() => GetPreviewEmitRotRad(in _previewEmitter);

    DanmakuConfig ResolvePreviewDanmakuConfig()
    {
        string id = ResolvePreviewDanmakuConfigId();
        if (string.IsNullOrEmpty(id))
            return null;

        return ConfigViewerAssetLookup.FindDanmakuConfig(id);
    }

    string ResolvePreviewDanmakuConfigId()
    {
        if (emitterConfig?.danmakuConfigIds == null || emitterConfig.danmakuConfigIds.Length == 0)
            return null;

        string[] ids = emitterConfig.danmakuConfigIds;
        return emitterConfig.danmakuSelectMode switch
        {
            DanmakuSelectMode.Sequential => PickSequentialPreviewDanmakuId(ids),
            DanmakuSelectMode.Random => PickFirstNonEmptyId(ids),
            _ => PickFirstNonEmptyId(ids),
        };
    }

    string PickSequentialPreviewDanmakuId(string[] ids)
    {
        for (int attempt = 0; attempt < ids.Length; attempt++)
        {
            int idx = (_previewSequentialBulletIndex + attempt) % ids.Length;
            string id = ids[idx];
            if (!string.IsNullOrWhiteSpace(id))
            {
                _previewSequentialBulletIndex = idx + 1;
                return id;
            }
        }

        return null;
    }

    static string PickFirstNonEmptyId(string[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(ids[i]))
                return ids[i];
        }

        return null;
    }

    CDanmakuEmitter BuildEmitterFromViewerFields()
    {
        var temp = ScriptableObject.CreateInstance<DanmakuEmitterConfig>();
        temp.emitMode = emitterType;
        temp.emitterCamp = emitterCamp;
        temp.emitterPosOffset = emitPosOffset;
        temp.emitterRotOffsetZ = emitRotOffsetZ;
        temp.danmakuRotOffsetZ = danmakuRotOffsetZ;
        temp.lineModeConfig = lineModeConfig;
        temp.arcModeConfig = arcModeConfig;
        temp.waveModeConfig = waveModeConfig;
        temp.grainModeConfig = grainModeConfig;
        temp.launchIntervalSeconds = launchIntervalSeconds;
        temp.launchCount = launchCount;
        temp.launchSpeed = launchSpeed;
        temp.audio_Fire = launchAudio;
        temp.displaySelfSpinDegreesPerSecond = displaySelfSpinDegreesPerSecond;
        temp.displayScaleMin = displayScaleMin;
        temp.displayScaleMax = displayScaleMax;
        temp.displayScaleCyclesPerSecond = displayScaleCyclesPerSecond;
        temp.aimAtPlayer = emitterConfig?.aimAtPlayer ?? false;
        if (emitterConfig != null)
        {
            temp.danmakuSelectMode = emitterConfig.danmakuSelectMode;
            temp.danmakuConfigIds = emitterConfig.danmakuConfigIds != null
                ? (string[])emitterConfig.danmakuConfigIds.Clone()
                : System.Array.Empty<string>();
            temp.salvoAngleAdvanceDeg = emitterConfig.salvoAngleAdvanceDeg;
        }

        temp.BakeLogicTiming(LogicFramePreviewClock.GetLogicFps());

        var emitter = new CDanmakuEmitter(temp);
        DestroyImmediate(temp);
        return emitter;
    }
#endif

    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        Vector3 origin = transform.position;

        if (!drawPreviewSpawnGizmos)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, 0.05f);

        if (emitterType == EmitMode.None)
            return;

        var samples = _lastBurstSamples;
        if (_previewActive || samples.Count == 0)
        {
            var emitter = BuildEmitterFromViewerFields();
            float emitRotRad = GetPreviewEmitRotRad(in emitter);
            samples = new List<DanmakuEmitterSpawnMath.SpawnSample>();
            uint gizmoFrame = _previewActive ? _previewClock.LogicFrame : 0;
            int salvoIndex = _previewActive ? _previewLaunchCountUsed : 0;
            DanmakuEmitterSpawnMath.CollectSpawns(
                in emitter, origin.x, origin.y, emitRotRad, samples, gizmoFrame, salvoIndex);
        }

        bool livePreview = _previewActive;
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            var pos = new Vector3(s.posX, s.posY, 0f);
            Gizmos.color = livePreview ? Color.yellow : new Color(0.4f, 0.85f, 1f, 0.85f);
            Gizmos.DrawWireSphere(pos, livePreview ? 0.04f : 0.035f);

            var velEnd = pos + new Vector3(s.velX, s.velY, 0f) * (livePreview ? 0.25f : 0.2f);
            Gizmos.color = livePreview
                ? new Color(1f, 0.4f, 0.1f, 0.9f)
                : new Color(0.4f, 0.85f, 1f, 0.85f);
            Gizmos.DrawLine(pos, velEnd);
        }
#endif
    }

}
