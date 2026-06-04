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
        public DanmakuEmitterConfig emitterCfg;
        public string spinStateKey;
        public Vector3 baseLocalScale;
    }

    struct DisplayMotionState
    {
        public float displaySpinAngleDeg;
        public float displayScalePhaseRad;
        public uint lastMotionLogicFrame;
    }

    readonly List<WeaponEmitLayout.EmitPoint> _points = new();
    readonly List<VisualEntry> _visuals = new();
    readonly Dictionary<string, DisplayMotionState> _displayMotionCache = new();

    Transform _layoutRoot;
    int _lastStructureHash = int.MinValue;
    int _lastPoseHash = int.MinValue;

    public void Clear()
    {
        _lastStructureHash = int.MinValue;
        _lastPoseHash = int.MinValue;
        _displayMotionCache.Clear();
        ReleaseVisuals();
    }

    public void Sync(
        Transform weaponTransform,
        WeaponConfig weapon,
        int powerOrbs,
        float secondaryConverge01,
        bool slowModePrimary,
        in EntityManager em,
        Entity ownerEntity)
    {
        if (weaponTransform == null || weapon == null)
        {
            Clear();
            return;
        }

        float rotRad = weaponTransform.eulerAngles.z * Mathf.Deg2Rad;
        if (weapon.ShouldUseSecondaryTrail(slowModePrimary) && em.IsValid(ownerEntity))
        {
            WeaponEmitLayout.CollectBattleWeaponVisualPoints(
                weaponTransform.position,
                rotRad,
                weapon,
                powerOrbs,
                secondaryConverge01,
                slowModePrimary,
                em,
                ownerEntity,
                _points);
        }
        else
        {
            WeaponEmitLayout.CollectBattleWeaponVisualPoints(
                weaponTransform.position,
                rotRad,
                weapon,
                powerOrbs,
                secondaryConverge01,
                slowModePrimary,
                _points);
        }

        int structureHash = ComputeStructureHash(weapon, powerOrbs, slowModePrimary, _points);
        int poseHash = ComputePoseHash(weaponTransform, secondaryConverge01, _points);

        if (structureHash != _lastStructureHash || _layoutRoot == null)
        {
            _lastStructureHash = structureHash;
            _lastPoseHash = poseHash;
            Rebuild(weaponTransform, _points);
            return;
        }

        if (poseHash != _lastPoseHash || HasActiveDisplayMotion(_points))
        {
            if (poseHash != _lastPoseHash)
                _lastPoseHash = poseHash;
            ApplyPose(_points);
        }
    }

    static bool HasActiveDisplayMotion(List<WeaponEmitLayout.EmitPoint> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var cfg = points[i].emitterCfg;
            if (cfg == null)
                continue;

            if (DanmakuEmitterDisplaySpin.HasDisplayMotion(
                    cfg.displaySelfSpinRadPerFrame,
                    cfg.displayScaleMin,
                    cfg.displayScaleMax,
                    cfg.displayScaleCyclesPerSecond))
                return true;
        }

        return false;
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
            DanmakuEmitterPresentation.Apply(point.emitterCfg, instance);

            _visuals.Add(new VisualEntry
            {
                go = instance,
                prefabIndex = point.emitterPrefabIndex,
                emitterCfg = point.emitterCfg,
                spinStateKey = GetSpinStateKey(point),
                baseLocalScale = Vector3.one,
            });
        }

        ApplyPose(points);
    }

    void ApplyPose(List<WeaponEmitLayout.EmitPoint> points)
    {
        uint logicFrame = ResolveLogicFrame();

        int count = Mathf.Min(_visuals.Count, points.Count);
        for (int i = 0; i < count; i++)
        {
            if (_visuals[i].go == null)
                continue;

            var point = points[i];
            var visual = _visuals[i];
            var cfg = visual.emitterCfg ?? point.emitterCfg;
            string motionKey = visual.spinStateKey ?? GetSpinStateKey(point);
            Transform t = visual.go.transform;

            if (cfg == null)
            {
                t.SetPositionAndRotation(
                    point.worldPosition,
                    Quaternion.Euler(0f, 0f, point.worldRotZDeg));
                t.localScale = visual.baseLocalScale;
                continue;
            }

            if (!_displayMotionCache.TryGetValue(motionKey, out var motion))
                motion = default;

            AdvanceDisplayMotion(ref motion, cfg, logicFrame);
            _displayMotionCache[motionKey] = motion;

            t.SetPositionAndRotation(
                point.worldPosition,
                DanmakuEmitterDisplaySpin.GetWorldRotation(point.worldRotZDeg, motion.displaySpinAngleDeg));
            t.localScale = DanmakuEmitterDisplaySpin.GetLocalScale(
                visual.baseLocalScale,
                cfg.displayScaleMin,
                cfg.displayScaleMax,
                motion.displayScalePhaseRad,
                cfg.displayScaleCyclesPerSecond);
        }
    }

    static string GetSpinStateKey(WeaponEmitLayout.EmitPoint point)
    {
        if (point.isPrimary)
            return "primary";

        if (!string.IsNullOrEmpty(point.label))
        {
            int modeSep = point.label.IndexOf('·');
            if (modeSep > 0)
                return point.label.Substring(0, modeSep);

            return point.label;
        }

        return point.emitterPrefabIndex.ToString();
    }

    static void AdvanceDisplayMotion(ref DisplayMotionState motion, DanmakuEmitterConfig cfg, uint logicFrame)
    {
        if (cfg == null)
            return;

        bool hasSpin = cfg.displaySelfSpinRadPerFrame != 0f;
        bool hasScale = DanmakuEmitterDisplaySpin.HasScalePulse(
            cfg.displayScaleMin,
            cfg.displayScaleMax,
            cfg.displayScaleCyclesPerSecond);

        if (!hasSpin && !hasScale)
            return;

        if (motion.lastMotionLogicFrame == 0)
        {
            motion.lastMotionLogicFrame = logicFrame;
            return;
        }

        if (logicFrame > motion.lastMotionLogicFrame)
        {
            uint delta = logicFrame - motion.lastMotionLogicFrame;
            if (hasSpin)
                motion.displaySpinAngleDeg += cfg.displaySelfSpinRadPerFrame * delta * Mathf.Rad2Deg;
            if (hasScale)
                motion.displayScalePhaseRad += cfg.displayScalePhaseRadPerFrame * delta;
        }

        motion.lastMotionLogicFrame = logicFrame;
    }

    static uint ResolveLogicFrame()
    {
        var world = BattleManager.Instance != null ? BattleManager.Instance.ActiveBattleWorld : null;
        return world != null ? world.LogicFrameTimer.CurrentFrame : 0u;
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
                var cfg = points[i].emitterCfg;
                h = h * 31 + (cfg != null ? cfg.GetInstanceID() : 0);
                h = h * 31 + (cfg != null && cfg.displaySprite != null ? cfg.displaySprite.GetInstanceID() : 0);
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
