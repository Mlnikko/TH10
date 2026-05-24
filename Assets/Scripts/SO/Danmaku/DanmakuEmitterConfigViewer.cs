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

    [Header("发射器类型")]
    [SerializeField] EmitMode emitterType;

    [Header("发射器阵营")]
    [SerializeField] EmitterCamp emitterCamp;

    [Header("发射器显示")]
    [SerializeField] Sprite displaySprite;

    [Header("发射器Offset调整")]
    [SerializeField] Vector2 emitPosOffset;
    [SerializeField] float emitRotOffsetZ;

    [Header("装填弹幕旋转")]
    [SerializeField] float danmakuRotOffsetZ;

    [Header("Line Mode 参数")]
    [SerializeField] LineModeConfig lineModeConfig;

    [Header("Arc Mode 参数")]
    [SerializeField] ArcModeConfig arcModeConfig;

    [Header("发射音效")]
    [SerializeField] AudioName launchAudio;

    [Header("发射间隔（秒）")]
    [SerializeField] float launchIntervalSeconds;

    [Header("发射速度")]
    [SerializeField] float launchSpeed;

#if UNITY_EDITOR
    [Header("编辑器预览")]
    [Tooltip("预览模拟时长（秒），按 GameManager.logicFPS 换算为逻辑帧")]
    [SerializeField] float previewDuration = 5f;

    [Tooltip("预览弹幕存活时间（秒），按逻辑帧计数后回收")]
    [SerializeField] float previewBulletLifetime = 3f;

    [SerializeField] bool drawPreviewSpawnGizmos = true;

    bool _previewActive;
    LogicFramePreviewRunner _previewClock;
    int _previewBulletLifetimeFrames;
    uint _previewLastFireFrame;
    CDanmakuEmitter _previewEmitter;
    int _previewSequentialBulletIndex;
    readonly List<DanmakuEmitterSpawnMath.SpawnSample> _lastBurstSamples = new();
    readonly List<PreviewBullet> _previewBullets = new();
    GameObject _previewRoot;

    struct PreviewBullet
    {
        public GameObject go;
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
        emitPosOffset = emitterConfig.emitterPosOffset;
        emitRotOffsetZ = emitterConfig.emitterRotOffsetZ;
        danmakuRotOffsetZ = emitterConfig.danmakuRotOffsetZ;
        lineModeConfig = emitterConfig.lineModeConfig;
        arcModeConfig = emitterConfig.arcModeConfig;
        launchIntervalSeconds = emitterConfig.launchIntervalSeconds;
        launchSpeed = emitterConfig.launchSpeed;
        launchAudio = emitterConfig.audio_Fire;

        ApplyEditorPreview();
        Logger.Debug("已加载发射器配置" + emitterConfig.name, LogTag.Config);
    }

    protected override void ApplyEditorPreview() => SyncDisplaySpriteFromConfig();

    /// <summary>将当前 <see cref="displaySprite"/> 写入预制体上的 SpriteRenderer（编辑器用）。</summary>
    public void SyncDisplaySpriteFromConfig()
    {
        if (!TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            return;

        spriteRenderer.sprite = displaySprite;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            SyncDisplaySpriteFromConfig();
    }
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
        emitterConfig.emitterPosOffset = emitPosOffset;
        emitterConfig.emitterRotOffsetZ = emitRotOffsetZ;
        emitterConfig.danmakuRotOffsetZ = danmakuRotOffsetZ;
        emitterConfig.lineModeConfig = lineModeConfig;
        emitterConfig.arcModeConfig = arcModeConfig;
        emitterConfig.launchIntervalSeconds = launchIntervalSeconds;
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

    protected override void StopEditorPreviews() => StopPreviewEmitter();

    public void StartPreviewEmitter()
    {
        StopPreviewEmitter();

        if (emitterConfig == null)
        {
            Logger.Warn("[DanmakuEmitterConfigViewer] 未指定 DanmakuEmitterConfig，无法预览。", LogTag.Config);
            return;
        }

        if (emitterType == EmitMode.None)
        {
            Logger.Warn("[DanmakuEmitterConfigViewer] 发射模式为 None，请改为 Line 或 Arc。", LogTag.Config);
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
        _previewSequentialBulletIndex = 0;
        _previewBulletLifetimeFrames = LogicFramePreviewClock.SecondsToLogicFrames(previewBulletLifetime, fps);
        _previewEmitter = BuildEmitterFromViewerFields();
        _previewActive = true;

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
        uint framesSinceLastFire = _previewClock.LogicFrame - _previewLastFireFrame;
        if (_previewEmitter.launchCooldownFrames > 0 &&
            framesSinceLastFire < (uint)_previewEmitter.launchCooldownFrames)
        {
            return;
        }

        var danmaku = ResolvePreviewDanmakuConfig();
        if (danmaku != null)
            FirePreviewBurst(danmaku);

        _previewLastFireFrame = _previewClock.LogicFrame;
    }

    void FirePreviewBurst(DanmakuConfig danmaku)
    {
        _previewEmitter = BuildEmitterFromViewerFields();

        Vector3 origin = transform.position;
        float emitRotRad = GetPreviewEmitRotRad();

        _lastBurstSamples.Clear();
        DanmakuEmitterSpawnMath.CollectSpawns(
            in _previewEmitter,
            origin.x,
            origin.y,
            emitRotRad,
            _lastBurstSamples);

        if (_lastBurstSamples.Count == 0)
        {
            Logger.Warn(
                "[DanmakuEmitterConfigViewer] 本次计算到 0 发弹幕：请检查 Line 的 lineCount 或 Arc 的 arcBulletCount。",
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

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = danmaku.sprite;
        sr.color = danmaku.color;
        ConfigViewerSpritePreview.ApplySortingFrom(go.transform, transform);

        float scale = Mathf.Max(danmaku.scale, 0.01f);
        go.transform.localScale = Vector3.one * scale;
        go.transform.position = new Vector3(x, y, transform.position.z);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotRad * Mathf.Rad2Deg);

        _previewBullets.Add(new PreviewBullet
        {
            go = go,
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

            b.ageFrames++;
            if (_previewBulletLifetimeFrames > 0 && b.ageFrames >= _previewBulletLifetimeFrames)
            {
                DestroyImmediate(b.go);
                _previewBullets.RemoveAt(i);
                continue;
            }

            Vector3 p = b.go.transform.position;
            p.x += b.velX;
            p.y += b.velY;
            b.go.transform.position = p;
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

    float GetPreviewEmitRotRad()
    {
        float baseRad = transform.eulerAngles.z * Mathf.Deg2Rad;
        return baseRad + _previewEmitter.emitterRotOffsetRad;
    }

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
        temp.launchIntervalSeconds = launchIntervalSeconds;
        temp.launchSpeed = launchSpeed;
        temp.audio_Fire = launchAudio;
        if (emitterConfig != null)
        {
            temp.danmakuSelectMode = emitterConfig.danmakuSelectMode;
            temp.danmakuConfigIds = emitterConfig.danmakuConfigIds != null
                ? (string[])emitterConfig.danmakuConfigIds.Clone()
                : System.Array.Empty<string>();
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

        if (_lastBurstSamples.Count > 0)
        {
            for (int i = 0; i < _lastBurstSamples.Count; i++)
            {
                var s = _lastBurstSamples[i];
                var pos = new Vector3(s.posX, s.posY, 0f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pos, 0.04f);

                var velEnd = pos + new Vector3(s.velX, s.velY, 0f) * 0.25f;
                Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
                Gizmos.DrawLine(pos, velEnd);
            }

            return;
        }

        if (emitterType == EmitMode.None)
            return;

        var emitter = BuildEmitterFromViewerFields();
        float emitRotRad = transform.eulerAngles.z * Mathf.Deg2Rad + emitter.emitterRotOffsetRad;

        var samples = new List<DanmakuEmitterSpawnMath.SpawnSample>();
        DanmakuEmitterSpawnMath.CollectSpawns(in emitter, origin.x, origin.y, emitRotRad, samples);

        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            var pos = new Vector3(s.posX, s.posY, 0f);
            Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.85f);
            Gizmos.DrawWireSphere(pos, 0.035f);
            Gizmos.DrawLine(pos, pos + new Vector3(s.velX, s.velY, 0f) * 0.2f);
        }
#endif
    }

}
