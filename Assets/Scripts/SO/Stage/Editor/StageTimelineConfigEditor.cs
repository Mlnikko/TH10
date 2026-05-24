#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageTimelineConfigViewer), true)]
public class StageTimelineConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var viewer = (StageTimelineConfigViewer)target;

        ConfigViewerEditorUI.DrawSeparator();
        EditorGUILayout.LabelField("关卡时间轴预览（仅编辑器）", EditorStyles.boldLabel);

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.stageTimelineConfig, "StageTimelineConfig"))
            return;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(StageTimelinePreviewRuntime.PlayModeRequiredMessage, MessageType.Warning);
        }
        else if (!StageTimelinePreviewRuntime.CanPreview)
        {
            EditorGUILayout.HelpBox(StageTimelinePreviewRuntime.InBattleBlockedMessage, MessageType.Warning);
        }
        else if (viewer.IsPreviewBootstrapping || StageTimelinePreviewRuntime.IsLoading)
        {
            EditorGUILayout.HelpBox("正在加载预览资源…", MessageType.Info);
        }
        else
        {
            ConfigViewerEditorUI.DrawPrefabSyncHint(
                "手动预览 StageTimeline，使用独立 ECS World，不影响正常进战流程。请指定 BattleAreaConfig 或留空使用 Manifest。");
        }

        EditorGUI.BeginDisabledGroup(
            !StageTimelinePreviewRuntime.CanPreview
            || viewer.IsPreviewBootstrapping
            || StageTimelinePreviewRuntime.IsLoading
            || viewer.IsPreviewingTimeline);

        if (GUILayout.Button("预览关卡时间轴", GUILayout.Height(28)))
            viewer.RequestPreviewStageTimeline();

        EditorGUI.EndDisabledGroup();

        if (viewer.IsPreviewingTimeline)
        {
            EditorGUILayout.HelpBox(
                $"正在预览：逻辑帧 {viewer.PreviewLogicFrame}，已播放 {viewer.PreviewElapsedSeconds:F1}s",
                MessageType.Info);

            if (GUILayout.Button("停止预览", GUILayout.Height(24)))
                viewer.StopPreviewTimeline();
        }
    }
}
#endif
