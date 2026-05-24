#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 计算武器发射点在 Scene 中的世界坐标（用于 Gizmo / 布局预览）。
/// </summary>
public static class ConfigViewerWeaponEmitLayout
{
    public struct EmitPoint
    {
        public string label;
        public Vector3 worldPosition;
        public bool isPrimary;
        public bool isSlowModeLayout;
        public Sprite displaySprite;
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
                CollectSecondaries(origin, rotRad, weapon, previewPowerOrbs, slowMode: false, outPoints);
                break;

            case WeaponEditorLayoutPreviewMode.SlowConvergeOnly:
                CollectPrimarySlow(origin, rotRad, weapon, includeSlowPrimaryEmitter, outPoints);
                CollectSecondaries(origin, rotRad, weapon, previewPowerOrbs, slowMode: true, outPoints);
                break;

            case WeaponEditorLayoutPreviewMode.Both:
                CollectPrimaryNormal(origin, rotRad, weapon, includeSlowPrimaryEmitter: false, outPoints);
                CollectPrimarySlow(origin, rotRad, weapon, includeSlowPrimaryEmitter, outPoints);
                CollectSecondaries(origin, rotRad, weapon, previewPowerOrbs, slowMode: false, outPoints);
                CollectSecondaries(origin, rotRad, weapon, previewPowerOrbs, slowMode: true, outPoints);
                break;
        }
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
        int previewPowerOrbs,
        bool slowMode,
        List<EmitPoint> outPoints)
    {
        if (!weapon.TryGetSecondarySlotsForPower(previewPowerOrbs, out var slots))
            return;

        string modeTag = slowMode ? "低速收束" : "通常";
        string powerTag = $"P{previewPowerOrbs}";

        for (int i = 0; i < slots.Length; i++)
        {
            Vector2 atMode = weapon.ResolveSecondarySlotOffset(slots[i].slotOffset, slowMode);
            TryAddPoint(
                outPoints,
                $"副炮[{i}]·{modeTag}·{powerTag}",
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

        var emitterCfg = ConfigViewerAssetLookup.FindDanmakuEmitterConfig(emitterConfigId);
        if (emitterCfg == null)
            return;

        outPoints.Add(new EmitPoint
        {
            label = label,
            worldPosition = GetEmitWorldPosition(origin, rotRad, emitterCfg, weaponSlotOffset),
            isPrimary = isPrimary,
            isSlowModeLayout = isSlowModeLayout,
            displaySprite = emitterCfg.displaySprite,
        });
    }
}
#endif
