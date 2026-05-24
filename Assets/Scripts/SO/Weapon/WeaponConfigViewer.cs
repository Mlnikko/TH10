using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] bool drawEmitterLayoutGizmos = true;
    [SerializeField] WeaponEditorLayoutPreviewMode previewLayoutMode = WeaponEditorLayoutPreviewMode.Both;
    [SerializeField] bool previewLayoutShowSlowPrimaryEmitter = true;
    [SerializeField, Min(0)] int previewPowerOrbs;

    [Header("发射预览")]
    [SerializeField, Min(0)] int previewFirePowerOrbs;
    [SerializeField] float previewDuration = 5f;
    [SerializeField] float previewBulletLifetime = 3f;
    [SerializeField] bool drawEmitterDisplaySprites = true;

    readonly ConfigViewerWeaponFirePreview _firePreview = new();
    readonly List<ConfigViewerWeaponEmitLayout.EmitPoint> _layoutPoints = new();
#endif

    public void LoadWeaponConfig() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (weaponConfig == null)
            return;

        weaponID = weaponConfig.weaponID;
        display = weaponConfig.display ?? new WeaponDisplayConfig();
        primaryEmitters = weaponConfig.primaryEmitters ?? new WeaponPrimaryEmitterGroup();
        powerSecondaryLayouts = weaponConfig.powerSecondaryLayouts ?? System.Array.Empty<WeaponPowerSecondaryLayout>();
        slowModeLayout = weaponConfig.slowModeLayout ?? new WeaponSlowModeLayoutConfig();
    }

    public void SaveWeaponConfig()
    {
        if (weaponConfig == null)
            return;

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

    protected override void StopEditorPreviews()
    {
        StopFirePreview();
    }

    public void StartFirePreview(WeaponEditorFirePreviewMode fireMode)
    {
        if (weaponConfig == null)
        {
            Logger.Warn("[WeaponConfigViewer] 未指定 WeaponConfig。", LogTag.Config);
            return;
        }

        _firePreview.Start(
            transform,
            BuildRuntimeWeaponSnapshot(),
            previewFirePowerOrbs,
            fireMode,
            previewDuration,
            previewBulletLifetime,
            nameof(WeaponConfigViewer));
    }

    public void StopFirePreview() => _firePreview.Stop();

    public WeaponConfig BuildRuntimeWeaponSnapshot()
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

    void OnDrawGizmosSelected()
    {
        if (!drawEmitterLayoutGizmos)
            return;

        var snap = BuildRuntimeWeaponSnapshot();
        float rotRad = transform.eulerAngles.z * Mathf.Deg2Rad;
        Vector3 origin = transform.position;

        ConfigViewerWeaponEmitLayout.CollectLayoutPoints(
            origin,
            rotRad,
            snap,
            previewPowerOrbs,
            previewLayoutMode,
            previewLayoutShowSlowPrimaryEmitter,
            _layoutPoints);

        DestroyImmediate(snap);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(origin, 0.06f);

        for (int i = 0; i < _layoutPoints.Count; i++)
        {
            var p = _layoutPoints[i];
            Gizmos.color = p.isSlowModeLayout
                ? new Color(1f, 0.85f, 0.2f, 0.95f)
                : p.isPrimary
                    ? new Color(0.35f, 0.9f, 1f, 0.95f)
                    : new Color(0.5f, 1f, 0.55f, 0.9f);

            Gizmos.DrawWireSphere(p.worldPosition, p.isPrimary ? 0.055f : 0.045f);
            Gizmos.DrawLine(origin, p.worldPosition);
            Handles.Label(p.worldPosition + Vector3.up * 0.08f, p.label);

            if (drawEmitterDisplaySprites && p.displaySprite != null)
            {
                var tint = p.isSlowModeLayout
                    ? new Color(1f, 0.9f, 0.45f, 0.75f)
                    : new Color(0.75f, 1f, 1f, 0.75f);
                ConfigViewerGizmoSprite.DrawAt(p.worldPosition, p.displaySprite, tint);
            }
        }
    }
#endif
}
