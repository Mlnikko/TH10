using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 配置 Viewer 基类：仅在编辑器中双击进入预制体隔离模式时，从 SO 自动同步到 Viewer 字段。
/// </summary>
public abstract class GameConfigViewerBase : MonoBehaviour
{
    protected abstract bool HasAssignedConfig { get; }

    public abstract void LoadFromConfig();

    /// <summary>编辑器中加载后刷新 Sprite 等 Scene 表现（默认无操作）。</summary>
    protected virtual void ApplyEditorPreview() { }

    /// <summary>停止编辑器预览（Player 构建中为空实现，子类仅在 <c>#if UNITY_EDITOR</c> 内重写）。</summary>
    protected virtual void StopEditorPreviews() { }

#if UNITY_EDITOR
    public void SyncFromConfigInEditor()
    {
        if (Application.isPlaying || !HasAssignedConfig)
            return;

        LoadFromConfig();
        ApplyEditorPreview();
    }

    [UnityEditor.InitializeOnLoadMethod]
    static void RegisterPrefabStageHook()
    {
        PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
    }

    static void OnPrefabStageOpened(PrefabStage stage)
    {
        if (stage?.prefabContentsRoot == null)
            return;

        var viewers = stage.prefabContentsRoot.GetComponentsInChildren<GameConfigViewerBase>(true);
        for (int i = 0; i < viewers.Length; i++)
            viewers[i].SyncFromConfigInEditor();
    }
#endif

    protected virtual void OnDisable()
    {
#if UNITY_EDITOR
        StopEditorPreviews();
#endif
    }
}
