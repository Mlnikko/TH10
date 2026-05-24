using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器发射点布局计算（编辑器预览与战斗武器表现共用）。
/// Power 副炮按档位<strong>切换</strong>整组槽位，不在低档基础上叠加。
/// </summary>
public static class WeaponEmitLayout
{
    public struct EmitPoint
    {
        public string label;
        public Vector3 worldPosition;
        public float worldRotZDeg;
        public bool isPrimary;
        public bool isSlowModeLayout;
        public DanmakuEmitterConfig emitterCfg;
        public int emitterPrefabIndex;
    }

    public static Vector2 RotateOffset(Vector2 offset, float rotRad)
    {
        float cos = Mathf.Cos(rotRad);
        float sin = Mathf.Sin(rotRad);
        return new Vector2(
            offset.x * cos - offset.y * sin,
            offset.x * sin + offset.y * cos);
    }

    public static Vector3 GetEmitWorldPosition(
        Vector3 origin,
        float rotRad,
        DanmakuEmitterConfig emitterCfg,
        Vector2 weaponSlotOffset)
    {
        if (emitterCfg == null)
            return origin;

        Vector2 total = emitterCfg.emitterPosOffset + weaponSlotOffset;
        Vector2 rotated = RotateOffset(total, rotRad);
        return origin + new Vector3(rotated.x, rotated.y, 0f);
    }

#if UNITY_EDITOR
    public static void CollectLayoutPoints(
        Vector3 origin,
        float rotRad,
        WeaponConfig weapon,
        int previewPowerOrbs,
        WeaponEditorLayoutPreviewMode mode,
        bool includeSlowPrimaryEmitter,
        List<EmitPoint> outPoints)
    {
        outPoints.Clear();
        if (weapon == null)
            return;

        switch (mode)
        {
            case WeaponEditorLayoutPreviewMode.NormalOnly:
                CollectPrimaryNormal(origin, rotRad, weapon, includeSlowPrimaryEmitter: false, outPoints);
                CollectSecondaries(origin, rotRad, weapon, previewPowerOrbs, slowMode: false, secondaryConverge01: 0f, outPoints);
                break;

            case WeaponEditorLayoutPreviewMode.SlowConvergeOnly:
                CollectPrimarySlow(origin, rotRad, weapon, includeSlowPrimaryEmitter, outPoints);
                CollectSecondaries(origin, rotRad, weapon, previewPowerOrbs, slowMode: true, secondaryConverge01: 1f, outPoints);
                break;

            case WeaponEditorLayoutPreviewMode.Both:
                CollectPrimaryNormal(origin, rotRad, weapon, includeSlowPrimaryEmitter: false, outPoints);
                CollectPrimarySlow(origin, rotRad, weapon, includeSlowPrimaryEmitter, outPoints);
                CollectSecondaries(origin, rotRad, weapon, previewPowerOrbs, slowMode: false, secondaryConverge01: 0f, outPoints);
                CollectSecondaries(origin, rotRad, weapon, previewPowerOrbs, slowMode: true, secondaryConverge01: 1f, outPoints);
                break;
        }
    }
#endif

    /// <summary>战斗时武器预制体表现：主炮 + 当前 Power 档副炮（整档替换，非叠加）。</summary>
    public static void CollectBattleWeaponVisualPoints(
        Vector3 origin,
        float rotRad,
        WeaponConfig weapon,
        int powerOrbs,
        float secondaryConverge01,
        bool slowModePrimary,
        List<EmitPoint> outPoints)
    {
        outPoints.Clear();
        if (weapon == null)
            return;

        if (slowModePrimary)
            CollectPrimarySlow(origin, rotRad, weapon, includeSlowPrimaryEmitter: true, outPoints);
        else
            CollectPrimaryNormal(origin, rotRad, weapon, includeSlowPrimaryEmitter: false, outPoints);

        CollectSecondaries(origin, rotRad, weapon, powerOrbs, slowModePrimary, secondaryConverge01, outPoints);
    }

    static void CollectPrimaryNormal(
        Vector3 origin,
        float rotRad,
        WeaponConfig weapon,
        bool includeSlowPrimaryEmitter,
        List<EmitPoint> outPoints)
    {
        TryAddPoint(
            outPoints,
            "主炮·通常",
            origin,
            rotRad,
            weapon.primaryEmitters.normal.danmakuEmitterConfigId,
            weapon.ResolvePrimarySlotOffset(slowMode: false),
            isPrimary: true,
            isSlowModeLayout: false);
    }

    static void CollectPrimarySlow(
        Vector3 origin,
        float rotRad,
        WeaponConfig weapon,
        bool includeSlowPrimaryEmitter,
        List<EmitPoint> outPoints)
    {
        string slowEmitterId = weapon.primaryEmitters.slowModeDanmakuEmitterConfigId;
        if (includeSlowPrimaryEmitter && !string.IsNullOrEmpty(slowEmitterId))
        {
            TryAddPoint(
                outPoints,
                "主炮·低速弹",
                origin,
                rotRad,
                slowEmitterId,
                weapon.ResolvePrimarySlotOffset(slowMode: true),
                isPrimary: true,
                isSlowModeLayout: true);
        }
        else
        {
            TryAddPoint(
                outPoints,
                "主炮·低速收束",
                origin,
                rotRad,
                weapon.primaryEmitters.normal.danmakuEmitterConfigId,
                weapon.ResolvePrimarySlotOffset(slowMode: true),
                isPrimary: true,
                isSlowModeLayout: true);
        }
    }

    static void CollectSecondaries(
        Vector3 origin,
        float rotRad,
        WeaponConfig weapon,
        int powerOrbs,
        bool slowMode,
        float secondaryConverge01,
        List<EmitPoint> outPoints)
    {
        if (!weapon.TryGetSecondarySlotsForPower(powerOrbs, out var slots))
            return;

        string modeTag = slowMode ? "低速" : "通常";

        for (int i = 0; i < slots.Length; i++)
        {
            Vector2 atMode = weapon.ResolveSecondarySlotOffset(slots[i].slotOffset, secondaryConverge01);
            TryAddPoint(
                outPoints,
                $"副炮[{i}]·{modeTag}",
                origin,
                rotRad,
                slots[i].danmakuEmitterConfigId,
                atMode,
                isPrimary: false,
                isSlowModeLayout: slowMode);
        }
    }

    static void TryAddPoint(
        List<EmitPoint> outPoints,
        string label,
        Vector3 origin,
        float rotRad,
        string emitterConfigId,
        Vector2 weaponSlotOffset,
        bool isPrimary,
        bool isSlowModeLayout)
    {
        emitterConfigId = StringHelper.NormalizeResourceId(emitterConfigId);
        if (string.IsNullOrEmpty(emitterConfigId))
            return;

        DanmakuEmitterConfig emitterCfg = null;
#if UNITY_EDITOR
        emitterCfg = ConfigViewerAssetLookup.FindDanmakuEmitterConfig(emitterConfigId);
#endif
        if (emitterCfg == null && GameResDB.Instance != null)
            emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterConfigId);

        if (emitterCfg == null)
            return;

        int prefabIndex = emitterCfg.emitterPrefabIndex;
        if (prefabIndex < 0 && GameResDB.Instance != null)
            prefabIndex = GameResDB.Instance.GetPrefabIndex(emitterCfg.emitterPrefabId);

        outPoints.Add(new EmitPoint
        {
            label = label,
            worldPosition = GetEmitWorldPosition(origin, rotRad, emitterCfg, weaponSlotOffset),
            worldRotZDeg = rotRad * Mathf.Rad2Deg + emitterCfg.emitterRotOffsetZ,
            isPrimary = isPrimary,
            isSlowModeLayout = isSlowModeLayout,
            emitterCfg = emitterCfg,
            emitterPrefabIndex = prefabIndex,
        });
    }
}
