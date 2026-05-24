/// <summary>武器配置 Viewer：Scene 布局 Gizmo 显示模式。</summary>
public enum WeaponEditorLayoutPreviewMode
{
    /// <summary>仅通常模式发射点（未收束）。</summary>
    NormalOnly = 0,

    /// <summary>仅低速收束后的发射点。</summary>
    SlowConvergeOnly = 1,

    /// <summary>同时显示通常与低速收束（分色）。</summary>
    Both = 2,
}

/// <summary>武器配置 Viewer：开火预览模式。</summary>
public enum WeaponEditorFirePreviewMode
{
    /// <summary>通常模式主炮 + 当前 Power 副炮。</summary>
    Normal = 0,

    /// <summary>低速主炮（若配置）+ 收束偏移 + 当前 Power 副炮。</summary>
    SlowConverge = 1,
}
