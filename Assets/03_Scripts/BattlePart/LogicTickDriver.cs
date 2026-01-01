using System;
public static class LogicTimer
{
    public const float LOGIC_DELTA_TIME = 0.02f;
    public static uint CurrentLogicFrame { get; private set; }
    public static void AdvanceLogicFrame() => CurrentLogicFrame++;
}

public class LogicTickDriver
{
    World _world;
    Func<uint, bool> _areInputsReady;
    Action<uint> _cleanupOldFrames;
    Action _onFrameAdvanced;

    public LogicTickDriver(World world, Func<uint, bool> areInputsReady, Action<uint> cleanupOldFrames, Action onFrameAdvanced = null)
    {
        _world = world;
        _areInputsReady = areInputsReady;
        _cleanupOldFrames = cleanupOldFrames;
        _onFrameAdvanced = onFrameAdvanced;
    }

    public bool TryAdvanceFrame()
    {
        uint frame = LogicTimer.CurrentLogicFrame;

        if (!_areInputsReady(frame))
            return false;

        _world.FixedUpdate(LogicTimer.LOGIC_DELTA_TIME);
        LogicTimer.AdvanceLogicFrame();
        _cleanupOldFrames(frame);
        _onFrameAdvanced?.Invoke();

        return true;
    }
}