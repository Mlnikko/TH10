using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 武器预制体编辑器：布局/发射预览。运行时由 <see cref="WeaponRuntimeLayoutView"/> 驱动表现，本组件在 Play 时自动禁用。
/// </summary>
public class WeaponConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => weaponConfig != null;

    public WeaponConfig WeaponConfig => weaponConfig;

    [Header("配置文件")]
    [SerializeField] WeaponConfig weaponConfig;

    [Header("武器")]
    [SerializeField] E_Weapon weaponID;

    [Header("显示")]
    [SerializeField] WeaponDisplayConfig display = new();

    [Header("主发射器")]
    [SerializeField] WeaponPrimaryEmitterGroup primaryEmitters = new();

    [Header("副发射器（按 Power）")]
    [SerializeField] WeaponPowerSecondaryLayout[] powerSecondaryLayouts = System.Array.Empty<WeaponPowerSecondaryLayout>();

    [Header("低速收束布局")]
    [SerializeField] WeaponSlowModeLayoutConfig slowModeLayout = new();

#if UNITY_EDITOR
    [Header("布局预览")]
    [SerializeField] WeaponEditorLayoutPreviewMode previewLayoutMode = WeaponEditorLayoutPreviewMode.Both;
    [SerializeField, Min(0)] int previewPowerOrbs;

    [Header("发射预览")]
    [SerializeField] float previewDuration = 5f;
    [SerializeField] float previewBulletLifetime = 3f;

    readonly ConfigViewerWeaponFirePreview _firePreview = new();
    readonly ConfigViewerWeaponLayoutPreview _layoutPreview = new();
    WeaponConfig _layoutPreviewSnapshot;
    bool _layoutPreviewSnapshotDirty = true;
#endif

    public void LoadWeaponConfig() => LoadFromConfig();

    protected override void ApplyEditorPreview() => RefreshEmitterLayoutPreview();

    public override void LoadFromConfig()
    {
        if (weaponConfig == null)
            return;

#if UNITY_EDITOR
        _layoutPreviewSnapshotDirty = true;
        _layoutPreview.Invalidate();
#endif

        weaponID = weaponConfig.weaponID;
        display = weaponConfig.display ?? new WeaponDisplayConfig();
        primaryEmitters = weaponConfig.primaryEmitters ?? new WeaponPrimaryEmitterGroup();
        powerSecondaryLayouts = weaponConfig.powerSecondaryLayouts ?? System.Array.Empty<WeaponPowerSecondaryLayout>();
        slowModeLayout = weaponConfig.slowModeLayout ?? new WeaponSlowModeLayoutConfig();

#if UNITY_EDITOR
        RefreshEmitterLayoutPreview();
#endif
    }

    public void SaveWeaponConfig()
    {
        if (weaponConfig == null)
            return;

#if UNITY_EDITOR
        _layoutPreviewSnapshotDirty = true;
        _layoutPreview.Invalidate();
#endif

        weaponConfig.weaponID = weaponID;
        weaponConfig.display = display;
        weaponConfig.primaryEmitters = primaryEmitters;
        weaponConfig.powerSecondaryLayouts = powerSecondaryLayouts;
        weaponConfig.slowModeLayout = slowModeLayout;
        weaponConfig.description = display.description;
    }

#if UNITY_EDITOR
    public bool IsPreviewingFire => _firePreview.IsActive;

    public void PreviewWeaponFire(WeaponEditorFirePreviewMode fireMode) => StartFirePreview(fireMode);

    public void InvalidateLayoutPreview()
    {
        _layoutPreviewSnapshotDirty = true;
        _layoutPreview.Invalidate();
    }

    public void RefreshEmitterLayoutPreview()
    {
        if (!ConfigViewerEditorScene.CanHostTransientPreview(transform))
        {
            _layoutPreview.Clear();
            return;
        }

        _layoutPreviewSnapshotDirty = true;

        _layoutPreview.Sync(
            transform,
            GetLayoutPreviewSnapshot(),
            previewPowerOrbs,
            previewLayoutMode);
    }

    protected override void StopEditorPreviews()
    {
        StopFirePreview();
        _layoutPreview.Clear();
        DestroyLayoutPreviewSnapshot();
    }

    void OnValidate()
    {
        _layoutPreviewSnapshotDirty = true;
        _layoutPreview.Invalidate();
    }

    void Update()
    {
        if (!ConfigViewerEditorScene.CanHostTransientPreview(transform))
        {
            if (_firePreview.IsActive)
                StopFirePreview();
            _layoutPreview.Clear();
            return;
        }

        RefreshEmitterLayoutPreview();
    }

    public void StartFirePreview(WeaponEditorFirePreviewMode fireMode)
    {
        if (weaponConfig == null)
        {
            Logger.Warn("[WeaponConfigViewer] 未指定 WeaponConfig。", LogTag.Config);
            return;
        }

        previewLayoutMode = fireMode == WeaponEditorFirePreviewMode.SlowConverge
            ? WeaponEditorLayoutPreviewMode.SlowConvergeOnly
            : WeaponEditorLayoutPreviewMode.NormalOnly;
        EditorUtility.SetDirty(this);

        _layoutPreviewSnapshotDirty = true;
        _layoutPreview.Invalidate();

        _firePreview.Start(
            transform,
            GetLayoutPreviewSnapshot(),
            previewPowerOrbs,
            fireMode,
            previewDuration,
            previewBulletLifetime,
            nameof(WeaponConfigViewer));
    }

    public void StopFirePreview() => _firePreview.Stop();

    WeaponConfig GetLayoutPreviewSnapshot()
    {
        if (!_layoutPreviewSnapshotDirty && _layoutPreviewSnapshot != null)
            return _layoutPreviewSnapshot;

        DestroyLayoutPreviewSnapshot();
        _layoutPreviewSnapshot = BuildRuntimeWeaponSnapshot();
        _layoutPreviewSnapshotDirty = false;
        return _layoutPreviewSnapshot;
    }

    void DestroyLayoutPreviewSnapshot()
    {
        if (_layoutPreviewSnapshot == null)
            return;

        DestroyImmediate(_layoutPreviewSnapshot);
        _layoutPreviewSnapshot = null;
    }

    WeaponConfig BuildRuntimeWeaponSnapshot()
    {
        var snap = Instantiate(weaponConfig);
        snap.weaponID = weaponID;
        snap.display = display;
        snap.primaryEmitters = primaryEmitters;
        snap.powerSecondaryLayouts = powerSecondaryLayouts;
        snap.slowModeLayout = slowModeLayout;
        snap.description = display.description;
        return snap;
    }
#endif
}
