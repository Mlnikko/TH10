using UnityEngine;

/// <summary>
/// 发射器 Sprite 自转/缩放（仅表现；不参与弹幕发射方向计算）。
/// </summary>
public static class DanmakuEmitterDisplaySpin
{
    public static bool HasScalePulse(float scaleMin, float scaleMax, float cyclesPerSecond) =>
        cyclesPerSecond > 0f && !Mathf.Approximately(scaleMin, scaleMax);

    public static bool HasDisplayMotion(
        float spinRadPerFrame,
        float scaleMin,
        float scaleMax,
        float scaleCyclesPerSecond) =>
        spinRadPerFrame != 0f || HasScalePulse(scaleMin, scaleMax, scaleCyclesPerSecond);

    public static Quaternion GetWorldRotation(float baseWorldRotZDeg, float displaySpinAngleDeg) =>
        Quaternion.Euler(0f, 0f, baseWorldRotZDeg + displaySpinAngleDeg);

    public static float GetUniformScale(float scaleMin, float scaleMax, float phaseRad)
    {
        float t = 0.5f + 0.5f * Mathf.Sin(phaseRad);
        return Mathf.Lerp(scaleMin, scaleMax, t);
    }

    public static Vector3 GetLocalScale(
        Vector3 baseLocalScale,
        float scaleMin,
        float scaleMax,
        float phaseRad,
        float scaleCyclesPerSecond)
    {
        if (!HasScalePulse(scaleMin, scaleMax, scaleCyclesPerSecond))
            return baseLocalScale;

        float multiplier = GetUniformScale(scaleMin, scaleMax, phaseRad);
        return baseLocalScale * multiplier;
    }
}
