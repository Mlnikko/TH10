#if UNITY_EDITOR
using System;
using System.Collections.Generic;

/// <summary>
/// 在时间轴 Viewer 内嵌编辑子配置时，由 Inspector 统一展示当前路径块并隐藏重复字段。
/// 使用栈支持嵌套 using，避免连续绘制中场/关底 Boss 时静态状态互相覆盖。
/// </summary>
public readonly struct StageTimelinePathEditScope : IDisposable
{
    struct ScopeFrame
    {
        public bool isActive;
        public StageTimelineConfigViewer viewer;
        public E_StageTimelinePathEditTarget target;
        public int waveIndex;
    }

    static readonly Stack<ScopeFrame> s_stack = new();

    public static bool IsActive => s_stack.Count > 0 && s_stack.Peek().isActive;
    public static StageTimelineConfigViewer Viewer => s_stack.Count > 0 ? s_stack.Peek().viewer : null;
    public static E_StageTimelinePathEditTarget Target =>
        s_stack.Count > 0 ? s_stack.Peek().target : E_StageTimelinePathEditTarget.MidStageWave;
    public static int WaveIndex => s_stack.Count > 0 ? s_stack.Peek().waveIndex : -1;

    /// <summary>内嵌 Inspector 已隐藏路径字段时，由 Viewer 顶部路径块统一编辑。</summary>
    public static bool ShouldHidePathRoutes(E_StageTimelinePathEditTarget target) =>
        IsActive
        && Target == target
        && Viewer != null
        && Viewer.PathEditTarget == target;

    public StageTimelinePathEditScope(
        StageTimelineConfigViewer viewer,
        E_StageTimelinePathEditTarget target,
        int waveIndex = 0)
    {
        s_stack.Push(new ScopeFrame
        {
            isActive = true,
            viewer = viewer,
            target = target,
            waveIndex = waveIndex,
        });
    }

    public void Dispose()
    {
        if (s_stack.Count > 0)
            s_stack.Pop();
    }
}
#endif
