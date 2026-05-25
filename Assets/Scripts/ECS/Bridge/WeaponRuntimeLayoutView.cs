using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗时同步武器预制体上的发射器表现：随 Power 档位整组切换副炮，不叠加旧档。
/// </summary>
public sealed class WeaponRuntimeLayoutView
{
    struct VisualEntry
    {
        public GameObject go;
        public int prefabIndex;
    }

    readonly List<WeaponEmitLayout.EmitPoint> _points = new();
    readonly List<VisualEntry> _visuals = new();

    Transform _layoutRoot;
    int _lastStructureHash = int.MinValue;
    int _lastPoseHash = int.MinValue;

    public void Clear()
    {
        _lastStructureHash = int.MinValue;
        _lastPoseHash = int.MinValue;
        ReleaseVisuals();
    }

    public void Sync(
        Transform weaponTransform,
        WeaponConfig weapon,
        int powerOrbs,
        float secondaryConverge01,
        bool slowModePrimary)
    {
        if (weaponTransform == null || weapon == null)
        {
            Clear();
            return;
        }

        float rotRad = weaponTransform.eulerAngles.z * Mathf.Deg2Rad;
        WeaponEmitLayout.CollectBattleWeaponVisualPoints(
            weaponTransform.position,
            rotRad,
            weapon,
            powerOrbs,
            secondaryConverge01,
            slowModePrimary,
            _points);

        int structureHash = ComputeStructureHash(weapon, powerOrbs, slowModePrimary, _points);
        int poseHash = ComputePoseHash(weaponTransform, secondaryConverge01, _points);

        if (structureHash != _lastStructureHash || _layoutRoot == null)
        {
            _lastStructureHash = structureHash;
            _lastPoseHash = poseHash;
            Rebuild(weaponTransform, _points);
            return;
        }

        if (poseHash != _lastPoseHash)
        {
            _lastPoseHash = poseHash;
            ApplyPose(_points);
        }
    }

    void Rebuild(Transform weaponTransform, List<WeaponEmitLayout.EmitPoint> points)
    {
        ReleaseVisuals();

        var rootGo = new GameObject("RuntimeWeaponLayout");
        rootGo.transform.SetParent(weaponTransform, false);
        rootGo.transform.localPosition = Vector3.zero;
        rootGo.transform.localRotation = Quaternion.identity;
        _layoutRoot = rootGo.transform;

        var pool = GameObjectPoolManager.Instance;
        if (pool == null)
            return;

        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            if (point.emitterPrefabIndex < 0)
                continue;

            GameObject instance = pool.Get(point.emitterPrefabIndex);
            if (instance == null)
                continue;

            instance.transform.SetParent(_layoutRoot, true);
            instance.name = point.label;
            instance.SetActive(true);

            _visuals.Add(new VisualEntry { go = instance, prefabIndex = point.emitterPrefabIndex });
        }

        ApplyPose(points);
    }

    void ApplyPose(List<WeaponEmitLayout.EmitPoint> points)
    {
        int count = Mathf.Min(_visuals.Count, points.Count);
        for (int i = 0; i < count; i++)
        {
            if (_visuals[i].go == null)
                continue;

            var point = points[i];
            _visuals[i].go.transform.SetPositionAndRotation(
                point.worldPosition,
                Quaternion.Euler(0f, 0f, point.worldRotZDeg));
        }
    }

    void ReleaseVisuals()
    {
        for (int i = 0; i < _visuals.Count; i++)
        {
            if (_visuals[i].go != null && GameObjectPoolManager.Instance != null)
                GameObjectPoolManager.Instance.Return(_visuals[i].go);
        }

        _visuals.Clear();

        if (_layoutRoot != null)
        {
            Object.Destroy(_layoutRoot.gameObject);
            _layoutRoot = null;
        }
    }

    static int ComputeStructureHash(WeaponConfig weapon, int powerOrbs, bool slowModePrimary, List<WeaponEmitLayout.EmitPoint> points)
    {
        int tierKey = weapon.TryResolvePowerSecondary(powerOrbs, out var tier) ? tier.minPowerOrbs : int.MinValue;

        unchecked
        {
            int h = 17;
            h = h * 31 + tierKey;
            h = h * 31 + (slowModePrimary ? 1 : 0);
            h = h * 31 + points.Count;

            for (int i = 0; i < points.Count; i++)
            {
                h = h * 31 + (points[i].label?.GetHashCode() ?? 0);
                h = h * 31 + points[i].emitterPrefabIndex;
            }

            return h;
        }
    }

    static int ComputePoseHash(Transform weaponTransform, float secondaryConverge01, List<WeaponEmitLayout.EmitPoint> points)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + Mathf.RoundToInt(secondaryConverge01 * 1000f);
            h = h * 31 + Mathf.RoundToInt(weaponTransform.position.x * 1000f);
            h = h * 31 + Mathf.RoundToInt(weaponTransform.position.y * 1000f);
            h = h * 31 + Mathf.RoundToInt(weaponTransform.eulerAngles.z * 10f);

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                h = h * 31 + Mathf.RoundToInt(p.worldPosition.x * 1000f);
                h = h * 31 + Mathf.RoundToInt(p.worldPosition.y * 1000f);
                h = h * 31 + Mathf.RoundToInt(p.worldRotZDeg * 10f);
            }

            return h;
        }
    }
}
