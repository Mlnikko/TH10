using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 配置 Viewer 基类：挂在<strong>编辑器预制体</strong>上，用于 SO ↔ Scene 同步与预览。
/// 运行时默认禁用本组件；战斗逻辑只读 <see cref="GameConfig"/>，不读 Viewer。
/// </summary>
[DisallowMultipleComponent]
public abstract class GameConfigViewerBase : MonoBehaviour
{
    /// <summary>
    /// 是否允许在 Play 模式下保持启用（仅编辑器内工具，如关卡时间轴分段预览）。
    /// 其余 Viewer 在进 Play 后自动 <c>enabled = false</c>，避免影响正式战斗流程。
    /// </summary>
    protected virtual bool AllowPlayModeExecution => false;

    protected abstract bool HasAssignedConfig { get; }

    public abstract void LoadFromConfig();

    /// <summary>编辑器中加载后刷新 Sprite 等 Scene 表现（默认无操作）。</summary>
    protected virtual void ApplyEditorPreview() { }

    /// <summary>停止编辑器预览（Player 构建中为空实现，子类仅在 <c>#if UNITY_EDITOR</c> 内重写）。</summary>
    protected virtual void StopEditorPreviews() { }

    protected virtual void Awake() => ApplyRuntimeGuard();

    protected virtual void OnEnable() => ApplyRuntimeGuard();

    void ApplyRuntimeGuard()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;

        if (!AllowPlayModeExecution)
            enabled = false;
#else
        enabled = false;
#endif
    }

#if UNITY_EDITOR
    public void SyncFromConfigInEditor()
    {
        if (Application.isPlaying && !AllowPlayModeExecution)
            return;

        if (!HasAssignedConfig)
            return;

        LoadFromConfig();
        ApplyEditorPreview();
    }

    public void StopAllEditorPreviews() => StopEditorPreviews();

    [UnityEditor.InitializeOnLoadMethod]
    static void RegisterPrefabStageHook()
    {
        PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
    }

    static void OnPrefabStageOpened(PrefabStage stage)
    {
        if (stage?.prefabContentsRoot == null)
            return;

        var viewers = stage.prefabContentsRoot.GetComponentsInChildren<GameConfigViewerBase>(true);
        for (int i = 0; i < viewers.Length; i++)
            viewers[i].SyncFromConfigInEditor();
    }

    static void OnPrefabStageClosing(PrefabStage stage)
    {
        if (stage?.prefabContentsRoot == null)
            return;

        var viewers = stage.prefabContentsRoot.GetComponentsInChildren<GameConfigViewerBase>(true);
        for (int i = 0; i < viewers.Length; i++)
            viewers[i].StopEditorPreviews();
    }
#endif

    protected virtual void OnDisable()
    {
#if UNITY_EDITOR
        StopEditorPreviews();
#endif
    }
}
