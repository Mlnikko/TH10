using UnityEngine;

/// <summary>
/// 战斗运行时指标：按真实时间窗口统计渲染帧率与实际逻辑帧率。
/// </summary>
public static class BattleRuntimeMetrics
{
    const float SampleWindowSeconds = 0.5f;

    static int _renderFrames;
    static int _logicTicks;
    static double _windowStartUnscaledTime;
    static float _renderFps;
    static float _logicFps;

    public static float RenderFps => _renderFps;
    public static float LogicFps => _logicFps;

    public static void Reset()
    {
        _renderFrames = 0;
        _logicTicks = 0;
        _renderFps = 0f;
        _logicFps = 0f;
        _windowStartUnscaledTime = Time.unscaledTimeAsDouble;
    }

    public static void RecordRenderFrame()
    {
        _renderFrames++;
        TryFlushWindow();
    }

    public static void RecordLogicTicks(int ticks)
    {
        if (ticks <= 0)
            return;

        _logicTicks += ticks;
        TryFlushWindow();
    }

    static void TryFlushWindow()
    {
        double now = Time.unscaledTimeAsDouble;
        double elapsed = now - _windowStartUnscaledTime;
        if (elapsed < SampleWindowSeconds)
            return;

        _renderFps = (float)(_renderFrames / elapsed);
        _logicFps = (float)(_logicTicks / elapsed);
        _renderFrames = 0;
        _logicTicks = 0;
        _windowStartUnscaledTime = now;
    }
}
