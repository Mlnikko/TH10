using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 挂载在掉落物预制体上：编辑 <see cref="DropItemConfig"/>、预览 Sprite / 碰撞体 Gizmo / 掉落运动轨迹。
/// </summary>
/// <summary>掉落物预制体配置编辑；不参与运行时逻辑。</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DropItemConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => dropItemConfig != null;
    [Header("配置文件")]
    public DropItemConfig dropItemConfig;

    [Header("表现")]
    [Tooltip("掉落物表现预制体 id（Drop 池）")]
    [PoolPrefabId(E_PoolCategory.Drop)]
    [SerializeField] string pickupPrefabId;

    [SerializeField] Sprite pickupSprite;

    [Header("出场运动")]
    [SerializeField] E_DropMotionMode motionMode;

    [Header("竖直上抛")]
    [SerializeField] float initialUpSpeed;
    [SerializeField] float fallGravity;
    [SerializeField] float maxFallSpeed;
    [SerializeField] float riseSpinDegreesPerSecond;

    [Header("定向散射后下落")]
    [SerializeField] float burstInitialSpeed;
    [SerializeField] Vector2 burstDirection;
    [SerializeField] float burstDeceleration;
    [SerializeField] float fallSpeedAfterBurst;

    [Header("碰撞")]
    [SerializeField] ColliderConfig colliderConfig;

    [Header("拾取效果")]
    [SerializeField] E_DropKind dropKind;

    [SerializeField] int effectAmount;

#if UNITY_EDITOR
    [Header("编辑器预览")]
    [Tooltip("场景内运动预览时长（秒），到时自动停止并复位")]
    [SerializeField] float previewMotionDuration = 4f;

    [Tooltip("是否在 Scene 视图绘制预览轨迹")]
    [SerializeField] bool drawPreviewMotionPath = true;

    bool _previewMotionActive;
    Vector3 _previewSpawnWorldPos;
    Quaternion _previewSpawnWorldRot;
    CDropItemMotion _previewMotion;
    LogicFramePreviewRunner _previewClock;
    float _previewOffsetX;
    float _previewOffsetY;
    float _previewAngleRad;
    Vector3[] _previewPathPoints;
    int _previewPathCount;
    const int MaxPreviewPathPoints = 512;
#endif

    public void LoadDropItemConfig() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (dropItemConfig == null)
        {
            Logger.Warn("掉落物配置文件未设置", LogTag.Config);
            return;
        }

        pickupPrefabId = dropItemConfig.pickupPrefabId;
        pickupSprite = dropItemConfig.pickupSprite;
        motionMode = dropItemConfig.motionMode;
        initialUpSpeed = dropItemConfig.initialUpSpeed;
        fallGravity = dropItemConfig.fallGravity;
        maxFallSpeed = dropItemConfig.maxFallSpeed;
        riseSpinDegreesPerSecond = dropItemConfig.riseSpinDegreesPerSecond;
        burstInitialSpeed = dropItemConfig.burstInitialSpeed;
        burstDirection = dropItemConfig.burstDirection;
        burstDeceleration = dropItemConfig.burstDeceleration;
        fallSpeedAfterBurst = dropItemConfig.fallSpeedAfterBurst;
        colliderConfig = dropItemConfig.colliderConfig;
        dropKind = dropItemConfig.dropKind;
        effectAmount = dropItemConfig.effectAmount;

        Logger.Debug($"掉落物配置加载完成: {dropItemConfig.name}");
    }

    public void SaveDropItemConfig()
    {
        if (dropItemConfig == null)
        {
            Logger.Warn("掉落物配置文件未设置", LogTag.Config);
            return;
        }

        dropItemConfig.pickupPrefabId = pickupPrefabId;
        dropItemConfig.pickupSprite = pickupSprite;
        dropItemConfig.motionMode = motionMode;
        dropItemConfig.initialUpSpeed = initialUpSpeed;
        dropItemConfig.fallGravity = fallGravity;
        dropItemConfig.maxFallSpeed = maxFallSpeed;
        dropItemConfig.riseSpinDegreesPerSecond = riseSpinDegreesPerSecond;
        dropItemConfig.burstInitialSpeed = burstInitialSpeed;
        dropItemConfig.burstDirection = burstDirection;
        dropItemConfig.burstDeceleration = burstDeceleration;
        dropItemConfig.fallSpeedAfterBurst = fallSpeedAfterBurst;
        dropItemConfig.colliderConfig = colliderConfig;
        dropItemConfig.dropKind = dropKind;
        dropItemConfig.effectAmount = effectAmount;

#if UNITY_EDITOR
        dropItemConfig.BakeLogicTiming(LogicFramePreviewClock.GetLogicFps());
#endif

        Logger.Debug($"掉落物配置保存完成: {dropItemConfig.name}");
    }

    protected override void ApplyEditorPreview() => ApplyDropItemSprite();

    void ApplyDropItemSprite()
    {
        if (dropItemConfig != null)
            DropItemPresentation.Apply(dropItemConfig, gameObject);
        else if (pickupSprite != null && TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            spriteRenderer.sprite = pickupSprite;
    }

    public void PreviewDropItem()
    {
        LoadFromConfig();
        ApplyDropItemSprite();
    }

#if UNITY_EDITOR
    public bool IsPreviewingDropMotion => _previewMotionActive;

    protected override void StopEditorPreviews() => StopPreviewDropMotion();

    void SyncViewerFieldsToConfig()
    {
        if (dropItemConfig == null)
            return;

        dropItemConfig.pickupPrefabId = pickupPrefabId;
        dropItemConfig.pickupSprite = pickupSprite;
        dropItemConfig.motionMode = motionMode;
        dropItemConfig.initialUpSpeed = initialUpSpeed;
        dropItemConfig.fallGravity = fallGravity;
        dropItemConfig.maxFallSpeed = maxFallSpeed;
        dropItemConfig.riseSpinDegreesPerSecond = riseSpinDegreesPerSecond;
        dropItemConfig.burstInitialSpeed = burstInitialSpeed;
        dropItemConfig.burstDirection = burstDirection;
        dropItemConfig.burstDeceleration = burstDeceleration;
        dropItemConfig.fallSpeedAfterBurst = fallSpeedAfterBurst;
        dropItemConfig.colliderConfig = colliderConfig;
        dropItemConfig.dropKind = dropKind;
        dropItemConfig.effectAmount = effectAmount;
    }

    public void StartPreviewDropMotion()
    {
        StopPreviewDropMotion();

        if (dropItemConfig == null)
        {
            Logger.Warn("[DropItemConfigViewer] 未指定 DropItemConfig，无法预览运动。", LogTag.Config);
            return;
        }

        SyncViewerFieldsToConfig();
        PreviewDropItem();

        uint fps = LogicFramePreviewClock.GetLogicFps();
        _previewMotion = DropItemMotionSimulator.CreateMotionFromConfig(dropItemConfig, fps);
        _previewClock = LogicFramePreviewClock.CreateRealTimeSession(previewMotionDuration, fps);
        _previewClock.Reset();
        _previewOffsetX = 0f;
        _previewOffsetY = 0f;
        _previewAngleRad = 0f;
        _previewSpawnWorldPos = transform.position;
        _previewSpawnWorldRot = transform.rotation;

        BakePreviewPath(fps);

        _previewMotionActive = true;
        EditorApplication.update -= OnEditorPreviewMotionUpdate;
        EditorApplication.update += OnEditorPreviewMotionUpdate;

        Logger.Info($"[DropItemConfigViewer] 掉落运动预览开始（{fps} 逻辑FPS，{previewMotionDuration:F1}s）。", LogTag.Config);
    }

    public void StopPreviewDropMotion()
    {
        if (!_previewMotionActive && _previewPathCount == 0)
            return;

        _previewMotionActive = false;
        EditorApplication.update -= OnEditorPreviewMotionUpdate;

        transform.position = _previewSpawnWorldPos;
        transform.rotation = _previewSpawnWorldRot;
        _previewOffsetX = 0f;
        _previewOffsetY = 0f;
        _previewAngleRad = 0f;

        SceneView.RepaintAll();
    }

    void OnEditorPreviewMotionUpdate()
    {
        if (!_previewMotionActive)
            return;

        if (this == null)
        {
            EditorApplication.update -= OnEditorPreviewMotionUpdate;
            return;
        }

        int steps = _previewClock.Tick(out bool stopped);
        if (stopped)
        {
            StopPreviewDropMotion();
            return;
        }

        for (int i = 0; i < steps; i++)
        {
            DropItemMotionSimulator.StepMotion(ref _previewMotion, out float dx, out float dy, out bool wasRising);
            _previewOffsetX += dx;
            _previewOffsetY += dy;

            var rotation = new CRotation(_previewAngleRad);
            DropItemMotionSimulator.StepAscentRotation(wasRising, in _previewMotion, ref rotation);
            _previewAngleRad = rotation.angleRad;
        }

        transform.position = _previewSpawnWorldPos + new Vector3(_previewOffsetX, _previewOffsetY, 0f);
        transform.rotation = _previewSpawnWorldRot * Quaternion.Euler(0f, 0f, _previewAngleRad * Mathf.Rad2Deg);
        if (steps > 0)
            SceneView.RepaintAll();
    }

    void BakePreviewPath(uint logicFps)
    {
        if (_previewPathPoints == null || _previewPathPoints.Length != MaxPreviewPathPoints)
            _previewPathPoints = new Vector3[MaxPreviewPathPoints];

        var motion = DropItemMotionSimulator.CreateMotionFromConfig(dropItemConfig, logicFps);
        float x = 0f;
        float y = 0f;
        _previewPathPoints[0] = _previewSpawnWorldPos;
        _previewPathCount = 1;

        int maxFrames = Mathf.Min(
            MaxPreviewPathPoints - 1,
            LogicFramePreviewClock.SecondsToLogicFrames(previewMotionDuration, logicFps));

        for (int frame = 0; frame < maxFrames; frame++)
        {
            DropItemMotionSimulator.StepMotion(ref motion, out float dx, out float dy, out _);
            x += dx;
            y += dy;
            _previewPathPoints[_previewPathCount++] = _previewSpawnWorldPos + new Vector3(x, y, 0f);
        }
    }

#endif

    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (drawPreviewMotionPath && _previewPathPoints != null && _previewPathCount >= 2)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            for (int i = 1; i < _previewPathCount; i++)
                Gizmos.DrawLine(_previewPathPoints[i - 1], _previewPathPoints[i]);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_previewPathPoints[0], 0.04f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_previewPathPoints[_previewPathCount - 1], 0.04f);
        }
#endif

        if (dropItemConfig == null)
            return;

        GizmosDrawer.ColliderDrawer(
            transform.position,
            transform.rotation,
            transform.localScale.x,
            colliderConfig,
            Color.cyan,
            Color.green);
    }

}
