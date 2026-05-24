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
        EditorGUILayout.LabelField("关卡时间轴预览", EditorStyles.boldLabel);

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.stageTimelineConfig, "StageTimelineConfig"))
            return;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(StageTimelinePreviewRuntime.PlayModeRequiredMessage, MessageType.Warning);
        }
        else if (viewer.IsPreviewBootstrapping || StageTimelinePreviewRuntime.IsLoading)
        {
            EditorGUILayout.HelpBox("正在初始化预览运行时（Addressables / GameResDB / 对象池）…", MessageType.Info);
        }
        else if (!StageTimelinePreviewRuntime.IsReady)
        {
            string err = StageTimelinePreviewRuntime.LastError;
            EditorGUILayout.HelpBox(
                string.IsNullOrEmpty(err)
                    ? "预览运行时未就绪。进入 Play 后会自动加载；也可点击下方按钮重试。"
                    : $"预览初始化失败：{err}",
                MessageType.Warning);
        }
        else
        {
            ConfigViewerEditorUI.DrawPrefabSyncHint(
                "Play 模式下按逻辑帧驱动 StageTimeline / 敌人运动 / 弹幕发射。请指定 BattleAreaConfig，或留空使用 Manifest 中的战斗区。");
        }

        EditorGUI.BeginDisabledGroup(
            !Application.isPlaying
            || viewer.IsPreviewBootstrapping
            || StageTimelinePreviewRuntime.IsLoading
            || viewer.IsPreviewingTimeline);

        if (GUILayout.Button(
                StageTimelinePreviewRuntime.IsReady ? "预览关卡时间轴" : "加载预览资源",
                GUILayout.Height(28)))
        {
            viewer.RequestPreviewStageTimeline();
        }

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
