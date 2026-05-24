#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 编辑器包装：<see cref="WeaponEmitLayout"/>。
/// </summary>
public static class ConfigViewerWeaponEmitLayout
{
    public struct EmitPoint
    {
        public string label;
        public Vector3 worldPosition;
        public float worldRotZDeg;
        public bool isPrimary;
        public bool isSlowModeLayout;
        public DanmakuEmitterConfig emitterCfg;
    }

    public static Vector2 RotateOffset(Vector2 offset, float rotRad) =>
        WeaponEmitLayout.RotateOffset(offset, rotRad);

    public static Vector3 GetEmitWorldPosition(
        Vector3 origin,
        float rotRad,
        DanmakuEmitterConfig emitterCfg,
        Vector2 weaponSlotOffset) =>
        WeaponEmitLayout.GetEmitWorldPosition(origin, rotRad, emitterCfg, weaponSlotOffset);

    public static void CollectLayoutPoints(
        Vector3 origin,
        float rotRad,
        WeaponConfig weapon,
        int previewPowerOrbs,
        WeaponEditorLayoutPreviewMode mode,
        bool includeSlowPrimaryEmitter,
        List<EmitPoint> outPoints)
    {
        var shared = new List<WeaponEmitLayout.EmitPoint>();
        WeaponEmitLayout.CollectLayoutPoints(
            origin,
            rotRad,
            weapon,
            previewPowerOrbs,
            mode,
            includeSlowPrimaryEmitter,
            shared);

        outPoints.Clear();
        for (int i = 0; i < shared.Count; i++)
        {
            var p = shared[i];
            outPoints.Add(new EmitPoint
            {
                label = p.label,
                worldPosition = p.worldPosition,
                worldRotZDeg = p.worldRotZDeg,
                isPrimary = p.isPrimary,
                isSlowModeLayout = p.isSlowModeLayout,
                emitterCfg = p.emitterCfg,
            });
        }
    }
}
#endif
