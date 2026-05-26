#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 武器配置 Viewer：在 Scene 中实例化 DanmakuEmitter 预制体以预览布局。
/// </summary>
public sealed class ConfigViewerWeaponLayoutPreview
{
    readonly List<ConfigViewerWeaponEmitLayout.EmitPoint> _points = new();

    GameObject _root;
    int _lastSyncHash;

    public void Invalidate() => _lastSyncHash = 0;

    public void Clear()
    {
        _lastSyncHash = 0;
        ConfigViewerEditorScene.DestroyRoot(ref _root);
    }

    public void Sync(
        Transform anchor,
        WeaponConfig weapon,
        int previewPowerOrbs,
        WeaponEditorLayoutPreviewMode mode)
    {
        if (anchor == null || weapon == null || !ConfigViewerEditorScene.CanHostTransientPreview(anchor))
        {
            Clear();
            return;
        }

        float rotRad = anchor.eulerAngles.z * Mathf.Deg2Rad;
        ConfigViewerWeaponEmitLayout.CollectLayoutPoints(
            anchor.position,
            rotRad,
            weapon,
            previewPowerOrbs,
            mode,
            includeSlowPrimaryEmitter: true,
            _points);

        int hash = ComputeLayoutHash(anchor, previewPowerOrbs, mode, _points);
        if (hash == _lastSyncHash && _root != null)
            return;

        _lastSyncHash = hash;
        RebuildFromPoints(anchor, _points);
    }

    void RebuildFromPoints(Transform anchor, List<ConfigViewerWeaponEmitLayout.EmitPoint> points)
    {
        ConfigViewerEditorScene.DestroyRoot(ref _root);

        string rootName = $"{anchor.name}_WeaponLayoutPreview";

        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var emitterCfg = point.emitterCfg;
            if (emitterCfg == null)
                continue;

            GameObject prefab = ConfigViewerAssetLookup.FindDanmakuEmitterPrefab(emitterCfg.emitterPrefabId);
            if (prefab == null)
                continue;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                continue;

            if (!ConfigViewerEditorScene.AttachTransientObject(instance, anchor, ref _root, rootName))
                continue;

            instance.name = point.label;
            instance.transform.SetPositionAndRotation(
                point.worldPosition,
                Quaternion.Euler(0f, 0f, point.worldRotZDeg));
            DanmakuEmitterPresentation.Apply(emitterCfg, instance);
            ApplyLayoutTint(instance, point.isSlowModeLayout);
        }

        SceneView.RepaintAll();
    }

    static void ApplyLayoutTint(GameObject instance, bool slowModeLayout)
    {
        var renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var sr = renderers[i];
            Color c = sr.color;
            sr.color = slowModeLayout
                ? new Color(1f, 0.92f, 0.45f, c.a > 0.01f ? c.a : 0.9f)
                : new Color(
                    Mathf.Min(c.r * 1.05f, 1f),
                    Mathf.Min(c.g * 1.05f, 1f),
                    Mathf.Min(c.b * 1.05f, 1f),
                    c.a > 0.01f ? c.a : 1f);
        }
    }

    static int ComputeLayoutHash(
        Transform anchor,
        int previewPowerOrbs,
        WeaponEditorLayoutPreviewMode mode,
        List<ConfigViewerWeaponEmitLayout.EmitPoint> points)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + previewPowerOrbs;
            h = h * 31 + (int)mode;
            h = h * 31 + Mathf.RoundToInt(anchor.position.x * 1000f);
            h = h * 31 + Mathf.RoundToInt(anchor.position.y * 1000f);
            h = h * 31 + Mathf.RoundToInt(anchor.eulerAngles.z * 10f);
            h = h * 31 + points.Count;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                h = h * 31 + (p.label?.GetHashCode() ?? 0);
                h = h * 31 + Mathf.RoundToInt(p.worldPosition.x * 1000f);
                h = h * 31 + Mathf.RoundToInt(p.worldPosition.y * 1000f);
                h = h * 31 + Mathf.RoundToInt(p.worldRotZDeg * 10f);
                h = h * 31 + (p.isSlowModeLayout ? 1 : 0);

                var cfg = p.emitterCfg;
                if (cfg != null)
                {
                    h = h * 31 + (cfg.emitterPrefabId?.GetHashCode() ?? 0);
                    h = h * 31 + cfg.emitterPosOffset.GetHashCode();
                    h = h * 31 + Mathf.RoundToInt(cfg.emitterRotOffsetZ * 10f);
                }
            }

            return h;
        }
    }
}
#endif
