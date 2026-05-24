using System;

/// <summary>
/// 独立的逻辑帧计时器（帧同步核心）
/// - 与 Unity 的 Update/FixedUpdate 解耦
/// - 提供 determinism 安全的时间推进
/// - 支持暂停、重置、追赶控制
/// </summary>
public class LogicFrameDriver
{
    public uint CurrentFrame { get; private set; } = 0;
    public double FrameIntervalSeconds; // 高精度版本
    double _accumulatedTime = 0.0;
    bool _isRunning = true;

    public LogicFrameDriver(uint logicFPS = 60)
    {
        if (logicFPS <= 0) throw new ArgumentException("FPS must be positive");
        FrameIntervalSeconds = 1.0 / logicFPS;
    }

    /// <summary>
    /// 累积真实经过的时间（通常在 MonoBehaviour.Update 中调用）
    /// </summary>
    public void AccumulateDeltaTime(float deltaTime)
    {
        if (_isRunning)
            _accumulatedTime += deltaTime;
    }

    /// <summary>
    /// 是否已累积足够时间以推进到下一逻辑帧？
    /// （只查询，不修改任何状态）
    /// </summary>
    public bool CanAdvance() => _accumulatedTime >= FrameIntervalSeconds;

    /// <summary>
    /// 强制推进一帧（由外部系统在确认输入就绪后调用）
    /// 不消耗 accumulatedTime，保留用于 catch-up 或动态调整
    /// </summary>
    public void AdvanceFrame() => CurrentFrame++;

    /// <summary>
    /// 手动消耗一帧的时间
    /// </summary>
    public void ConsumeFrameTime()
    {
        if (_accumulatedTime >= FrameIntervalSeconds)
            _accumulatedTime -= FrameIntervalSeconds;
    }

    public void ResetToFrame(uint frame)
    {
        CurrentFrame = frame;
        _accumulatedTime = 0.0; // 清空时间，从零开始累积
    }

    public void Pause() => _isRunning = false;
    public void Resume() => _isRunning = true;
    public bool IsRunning => _isRunning;

    public double GetAccumulatedTime() => _accumulatedTime;

    /// <summary>当前逻辑帧内已过时间比例 [0, 1]；用于 pos + velocity×α 的帧内平滑。</summary>
    public float GetRenderAlpha()
    {
        if (FrameIntervalSeconds <= 0.0)
            return 1f;
        double alpha = _accumulatedTime / FrameIntervalSeconds;
        return (float)(alpha < 1.0 ? alpha : 1.0);
    }

    /// <summary>超出单帧间隔的累积时间比例（锁步等待远程输入时用于本地预测）。</summary>
    public float GetOvershootAlpha()
    {
        if (FrameIntervalSeconds <= 0.0)
            return 0f;
        double t = _accumulatedTime / FrameIntervalSeconds;
        return (float)(t > 1.0 ? t - 1.0 : 0.0);
    }
}