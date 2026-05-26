#if UNITY_EDITOR
using System;

/// <summary>
/// 在时间轴 Viewer 内嵌编辑子配置时，由 Inspector 统一展示当前路径块并隐藏重复字段。
/// </summary>
public readonly struct StageTimelinePathEditScope : IDisposable
{
    public static bool IsActive { get; private set; }
    public static StageTimelineConfigViewer Viewer { get; private set; }
    public static E_StageTimelinePathEditTarget Target { get; private set; }
    public static int WaveIndex { get; private set; }

    public StageTimelinePathEditScope(
        StageTimelineConfigViewer viewer,
        E_StageTimelinePathEditTarget target,
        int waveIndex = 0)
    {
        IsActive = true;
        Viewer = viewer;
        Target = target;
        WaveIndex = waveIndex;
    }

    public void Dispose()
    {
        IsActive = false;
        Viewer = null;
        Target = E_StageTimelinePathEditTarget.MidStageWave;
        WaveIndex = -1;
    }
}
#endif
