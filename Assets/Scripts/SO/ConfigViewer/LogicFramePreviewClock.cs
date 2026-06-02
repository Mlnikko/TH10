#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器配置预览：逻辑 FPS 与真实时间累积 → 离散逻辑帧（与 <see cref="LogicFrameDriver"/> 一致）。
/// </summary>
public static class LogicFramePreviewClock
{
    public const uint DefaultLogicFps = 60;

    public static uint GetLogicFps()
    {
        if (GameManager.logicFPS > 0)
            return GameManager.logicFPS;
        return DefaultLogicFps;
    }

    public static float GetLogicStepSeconds(uint logicFps = 0)
    {
        if (logicFps == 0)
            logicFps = GetLogicFps();
        return 1f / logicFps;
    }

    public static int SecondsToLogicFrames(float seconds, uint logicFps = 0)
    {
        if (logicFps == 0)
            logicFps = GetLogicFps();
        return Mathf.Max(0, Mathf.CeilToInt(seconds * logicFps));
    }

    /// <summary>
    /// 按真实秒数结束预览，期间可消费多枚逻辑帧。
    /// </summary>
    public static LogicFramePreviewRunner CreateRealTimeSession(float durationSeconds, uint logicFps = 0)
    {
        if (logicFps == 0)
            logicFps = GetLogicFps();

        return new LogicFramePreviewRunner
        {
            LogicFps = logicFps,
            LogicStepSeconds = 1f / logicFps,
            MaxRealSeconds = durationSeconds,
            MaxLogicFrames = 0,
        };
    }

    /// <summary>
    /// 按逻辑帧数结束预览（时长秒数会换算为逻辑帧上限）。
    /// </summary>
    public static LogicFramePreviewRunner CreateLogicFrameSession(float durationSeconds, uint logicFps = 0)
    {
        if (logicFps == 0)
            logicFps = GetLogicFps();

        return new LogicFramePreviewRunner
        {
            LogicFps = logicFps,
            LogicStepSeconds = 1f / logicFps,
            MaxRealSeconds = -1f,
            MaxLogicFrames = (uint)SecondsToLogicFrames(durationSeconds, logicFps),
        };
    }
}

/// <summary>
/// 单次编辑器预览的时间轴状态（真实时间累积 + 逻辑帧步进）。
/// </summary>
public struct LogicFramePreviewRunner
{
    public uint LogicFps;
    public float LogicStepSeconds;
    public float MaxRealSeconds;
    public uint MaxLogicFrames;

    float _frameAccumSeconds;
    double _lastRealTimeSeconds;
    float _elapsedRealSeconds;
    uint _logicFrame;

    public uint LogicFrame => _logicFrame;
    public float ElapsedRealSeconds => _elapsedRealSeconds;

    public float RemainingRealSeconds =>
        MaxRealSeconds > 0f ? Mathf.Max(0f, MaxRealSeconds - _elapsedRealSeconds) : -1f;

    public void SetTotalMaxRealSeconds(float totalSeconds)
    {
        if (MaxRealSeconds <= 0f)
            return;

        MaxRealSeconds = Mathf.Max(_elapsedRealSeconds + 0.1f, totalSeconds);
    }

    public void Reset()
    {
        _frameAccumSeconds = 0f;
        _lastRealTimeSeconds = 0d;
        _elapsedRealSeconds = 0f;
        _logicFrame = 0;
    }

    /// <summary>返回本 tick 应执行的逻辑帧步数；<paramref name="stopped"/> 为 true 时表示预览应结束。</summary>
    public int Tick(out bool stopped)
    {
        stopped = false;

        double now = EditorApplication.timeSinceStartup;
        float realDt = _lastRealTimeSeconds > 0d
            ? (float)(now - _lastRealTimeSeconds)
            : 0f;
        _lastRealTimeSeconds = now;

        if (realDt > 0f)
        {
            _elapsedRealSeconds += realDt;
            _frameAccumSeconds += realDt;
        }

        if (MaxRealSeconds > 0f && _elapsedRealSeconds >= MaxRealSeconds)
        {
            stopped = true;
            return 0;
        }

        int steps = 0;
        while (_frameAccumSeconds >= LogicStepSeconds)
        {
            _frameAccumSeconds -= LogicStepSeconds;
            steps++;
            _logicFrame++;

            if (MaxLogicFrames > 0 && _logicFrame >= MaxLogicFrames)
            {
                stopped = true;
                break;
            }
        }

        return steps;
    }
}
#endif
