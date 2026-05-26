using UnityEngine;

/// <summary>
/// 渲染帧与逻辑帧之间的表现时钟（由 <see cref="BattleManager"/> 每帧同步）。
/// </summary>
public static class PresentationRuntime
{
    /// <summary>联机时启用插值/预测；单机直接对齐逻辑坐标。</summary>
    public static bool SmoothingEnabled { get; private set; }

    /// <summary>当前逻辑帧内的插值系数 [0,1]（距上一逻辑帧确认时刻的进度）。</summary>
    public static float LogicFrameAlpha { get; private set; }

    /// <summary>时间已到但联机输入未齐，本渲染帧未推进逻辑帧。</summary>
    public static bool IsLogicStalled { get; private set; }

    public static void SetSmoothingEnabled(bool enabled) => SmoothingEnabled = enabled;

    public static void Sync(LogicFrameDriver driver, bool logicStalledThisRenderFrame)
    {
        double interval = driver.FrameIntervalSeconds;
        LogicFrameAlpha = interval > 1e-9
            ? Mathf.Clamp01((float)(driver.GetAccumulatedTime() / interval))
            : 0f;

        IsLogicStalled = logicStalledThisRenderFrame && driver.CanAdvance();
    }

    public static void Reset()
    {
        SmoothingEnabled = false;
        LogicFrameAlpha = 0f;
        IsLogicStalled = false;
    }
}
