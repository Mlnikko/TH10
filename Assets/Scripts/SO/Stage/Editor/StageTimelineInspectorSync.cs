#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Scene / 外部直接改子配置后，同步嵌套 Inspector 的 SerializedObject 并重绘。</summary>
[InitializeOnLoad]
static class StageTimelineInspectorSync
{
    static StageTimelineInspectorSync()
    {
        StageTimelineConfigViewer.EmbeddedConfigChangedHook = SyncFromExternalEdit;
    }

    static void SyncFromExternalEdit(StageTimelineConfigViewer viewer, Object changedAsset)
    {
        if (changedAsset != null)
            SyncEditorForTarget(changedAsset);

        if (viewer != null)
            SyncEditorForTarget(viewer);
    }

    static void SyncEditorForTarget(Object target)
    {
        StageTimelineEmbeddedConfigEditor.SyncCachedEditor(target);

        Editor[] tracked = ActiveEditorTracker.sharedTracker.activeEditors;
        for (int i = 0; i < tracked.Length; i++)
        {
            Editor editor = tracked[i];
            if (editor == null || editor.target != target)
                continue;

            editor.serializedObject.Update();
            editor.Repaint();
        }
    }
}
#endif
