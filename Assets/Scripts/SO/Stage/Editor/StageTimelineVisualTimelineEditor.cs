#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="StageTimelineConfigViewer"/> Inspector 内的可视化时间线（拖拽调整开始时刻）。
/// </summary>
static class StageTimelineVisualTimelineEditor
{
    const string FoldoutPrefKey = "TH10.StageTimeline.VisualTimelineFoldout";
    const float LabelColumnWidth = 108f;
    const float RulerHeight = 18f;
    const float RowHeight = 22f;
    const float TrackPadding = 4f;
    const float MinBarWidthPx = 6f;

    static readonly List<StageTimelineVisualSchedule.Clip> s_clips = new(32);

    static int _dragClipListIndex = -1;
    static float _dragGrabOffsetSeconds;
    static Vector2 s_scrollPosition;

    public static void Draw(StageTimelineConfigViewer viewer, SerializedObject viewerSo)
    {
        if (viewer == null || viewer.stageTimelineConfig == null)
            return;

        bool expanded = EditorPrefs.GetBool(FoldoutPrefKey, true);
        expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, "可视化时间线");
        EditorPrefs.SetBool(FoldoutPrefKey, expanded);

        if (!expanded)
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawTimelineSettings(viewer, viewerSo);

            uint fps = LogicFramePreviewClock.GetLogicFps();
            var timeline = viewer.stageTimelineConfig;
            timeline.BakeLogicTiming(fps);
            StageTimelineVisualSchedule.CollectClips(timeline, fps, s_clips);

            float totalSeconds = StageTimelineVisualSchedule.ResolveTimelineDurationSeconds(
                timeline,
                viewer.TimelineViewDurationSeconds,
                fps);

            if (s_clips.Count == 0)
            {
                EditorGUILayout.HelpBox("当前时间轴没有可显示的波次或 Boss。", MessageType.None);
            }
            else
            {
                DrawTimelineTrack(viewer, timeline, fps, totalSeconds);
            }

            EditorGUILayout.HelpBox(
                "条块长度由各自内部配置（路径、在场时间、符卡阶段等）决定；水平拖拽条块可调整开始时刻。",
                MessageType.None);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    static void DrawTimelineSettings(StageTimelineConfigViewer viewer, SerializedObject viewerSo)
    {
        viewerSo.Update();
        var viewDur = viewerSo.FindProperty("timelineViewDurationSeconds");
        var pxPerSec = viewerSo.FindProperty("timelinePixelsPerSecond");

        EditorGUILayout.LabelField("时间轴显示", EditorStyles.miniBoldLabel);

        var timeline = viewer.stageTimelineConfig;
        uint fps = LogicFramePreviewClock.GetLogicFps();
        float contentEnd = timeline != null
            ? StageTimelineVisualSchedule.GetContentEndSeconds(timeline, fps)
            : 0f;
        float suggested = timeline != null
            ? StageTimelineVisualSchedule.GetSuggestedViewDurationSeconds(timeline, fps)
            : 30f;
        float resolved = timeline != null
            ? StageTimelineVisualSchedule.ResolveTimelineDurationSeconds(
                timeline,
                viewDur != null ? viewDur.floatValue : 0f,
                fps)
            : 30f;

        if (viewDur != null)
        {
            bool useAuto = viewDur.floatValue <= 0f;
            float sliderMax = Mathf.Max(
                300f,
                suggested * 1.5f,
                timeline != null && timeline.maxStageDurationSeconds > 0f
                    ? timeline.maxStageDurationSeconds
                    : 0f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("显示总时长");
                bool newAuto = EditorGUILayout.ToggleLeft("自动", useAuto, GUILayout.Width(52f));
                if (newAuto != useAuto)
                    viewDur.floatValue = newAuto ? 0f : suggested;

                EditorGUI.BeginDisabledGroup(viewDur.floatValue <= 0f);
                float manual = EditorGUILayout.Slider(
                    Mathf.Max(10f, viewDur.floatValue),
                    10f,
                    sliderMax);
                EditorGUI.EndDisabledGroup();

                if (viewDur.floatValue > 0f)
                    viewDur.floatValue = manual;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("自动", EditorStyles.miniButton))
                    viewDur.floatValue = 0f;
                if (GUILayout.Button("匹配内容", EditorStyles.miniButton))
                    viewDur.floatValue = suggested;
                if (timeline != null && timeline.maxStageDurationSeconds > 0f
                    && GUILayout.Button("关卡上限", EditorStyles.miniButton))
                    viewDur.floatValue = timeline.maxStageDurationSeconds;
            }

            string hint = useAuto
                ? $"当前标尺：{resolved:0.#} s（自动；内容至 {contentEnd:0.#} s）"
                : $"当前标尺：{resolved:0.#} s（内容至 {contentEnd:0.#} s）";
            EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
        }

        if (pxPerSec != null)
            EditorGUILayout.PropertyField(pxPerSec, new GUIContent("横向缩放 (px/秒)"));

        viewerSo.ApplyModifiedProperties();
    }

    static void DrawTimelineTrack(
        StageTimelineConfigViewer viewer,
        StageTimelineConfig timeline,
        uint fps,
        float totalSeconds)
    {
        totalSeconds = Mathf.Max(1f, totalSeconds);
        float pxPerSec = Mathf.Clamp(viewer.TimelinePixelsPerSecond, 4f, 48f);
        float trackWidth = totalSeconds * pxPerSec;
        float viewHeight = RulerHeight + TrackPadding * 2f + s_clips.Count * RowHeight;

        float contentWidth = LabelColumnWidth + TrackPadding * 2f + trackWidth + 12f;
        s_scrollPosition = EditorGUILayout.BeginScrollView(s_scrollPosition, GUILayout.Height(viewHeight));
        EditorGUILayout.BeginVertical(GUILayout.Width(contentWidth));

        var outer = EditorGUILayout.GetControlRect(false, viewHeight, GUILayout.Width(contentWidth));
        GUI.Box(outer, GUIContent.none, EditorStyles.helpBox);

        var trackRect = new Rect(
            outer.x + LabelColumnWidth + TrackPadding,
            outer.y + TrackPadding,
            trackWidth,
            outer.height - TrackPadding * 2f);

        GUIUtility.GetControlID("StageTimelineVisual".GetHashCode(), FocusType.Passive, outer);

        DrawRuler(trackRect, totalSeconds, pxPerSec);
        HandleDrag(viewer, timeline, fps, trackRect, totalSeconds, pxPerSec);

        float y = trackRect.y + RulerHeight;
        for (int i = 0; i < s_clips.Count; i++)
        {
            var clip = s_clips[i];
            var rowRect = new Rect(outer.x, y, contentWidth, RowHeight);
            var labelRect = new Rect(outer.x + 4f, y + 2f, LabelColumnWidth - 8f, RowHeight - 4f);
            var barRect = BuildBarRect(trackRect, clip, i, totalSeconds, pxPerSec);

            DrawRowBackground(rowRect, i, clip, viewer);
            EditorGUI.LabelField(labelRect, clip.Label, EditorStyles.miniLabel);
            DrawClipBar(barRect, clip, i == _dragClipListIndex);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                && barRect.Contains(Event.current.mousePosition) && _dragClipListIndex < 0)
            {
                SelectClip(viewer, clip);
                _dragClipListIndex = i;
                _dragGrabOffsetSeconds = (Event.current.mousePosition.x - barRect.x) / pxPerSec;
                Event.current.Use();
            }

            y += RowHeight;
        }

        if (Event.current.type == EventType.MouseUp && _dragClipListIndex >= 0)
        {
            _dragClipListIndex = -1;
            Event.current.Use();
        }

        if (_dragClipListIndex >= 0)
            HandleUtility.Repaint();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    static void DrawRuler(Rect trackRect, float totalSeconds, float pxPerSec)
    {
        var rulerRect = new Rect(trackRect.x, trackRect.y, trackRect.width, RulerHeight);
        EditorGUI.DrawRect(rulerRect, new Color(0.15f, 0.15f, 0.15f, 0.35f));

        int majorStep = totalSeconds > 120f ? 20 : (totalSeconds > 60f ? 10 : 5);
        for (float t = 0f; t <= totalSeconds + 0.01f; t += majorStep)
        {
            float x = rulerRect.x + t * pxPerSec;
            if (x > rulerRect.xMax)
                break;

            Handles.color = new Color(1f, 1f, 1f, 0.25f);
            Handles.DrawLine(new Vector3(x, rulerRect.y + 4f, 0f), new Vector3(x, rulerRect.yMax, 0f));
            GUI.Label(new Rect(x + 2f, rulerRect.y, 48f, 16f), $"{t:0}s", EditorStyles.miniLabel);
        }
    }

    static Rect BuildBarRect(
        Rect trackRect,
        StageTimelineVisualSchedule.Clip clip,
        int rowIndex,
        float totalSeconds,
        float pxPerSec)
    {
        float x = trackRect.x + clip.StartSeconds * pxPerSec;
        float w = clip.DurationSeconds / totalSeconds * trackRect.width;
        w = Mathf.Max(MinBarWidthPx, w);
        float y = trackRect.y + RulerHeight + rowIndex * RowHeight + 3f;
        return new Rect(x, y, w, RowHeight - 6f);
    }

    static void DrawRowBackground(Rect rowRect, int rowIndex, StageTimelineVisualSchedule.Clip clip, StageTimelineConfigViewer viewer)
    {
        if (IsClipSelected(viewer, clip))
            EditorGUI.DrawRect(rowRect, new Color(0.25f, 0.45f, 0.85f, 0.12f));
        else if (rowIndex % 2 == 1)
            EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.03f));
    }

    static void DrawClipBar(Rect barRect, StageTimelineVisualSchedule.Clip clip, bool dragging)
    {
        Color color = clip.Kind switch
        {
            StageTimelineVisualSchedule.ClipKind.MidBoss => new Color(0.95f, 0.55f, 0.2f, dragging ? 1f : 0.85f),
            StageTimelineVisualSchedule.ClipKind.MainBoss => new Color(0.9f, 0.25f, 0.3f, dragging ? 1f : 0.85f),
            _ => new Color(0.35f, 0.65f, 0.95f, dragging ? 1f : 0.85f),
        };

        EditorGUI.DrawRect(barRect, color);
        Handles.color = new Color(0f, 0f, 0f, 0.35f);
        Handles.DrawWireCube(barRect.center, barRect.size);

        var label = $"{clip.StartSeconds:0.#}s · {clip.DurationSeconds:0.#}s";

        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
        };
        if (barRect.width > 40f)
            GUI.Label(barRect, label, style);
    }

    static void HandleDrag(
        StageTimelineConfigViewer viewer,
        StageTimelineConfig timeline,
        uint fps,
        Rect trackRect,
        float totalSeconds,
        float pxPerSec)
    {
        if (_dragClipListIndex < 0 || _dragClipListIndex >= s_clips.Count)
            return;

        if (Event.current.type != EventType.MouseDrag && Event.current.type != EventType.MouseUp)
            return;

        var clip = s_clips[_dragClipListIndex];
        float mouseT = (Event.current.mousePosition.x - trackRect.x) / pxPerSec - _dragGrabOffsetSeconds;
        float maxStart = Mathf.Max(0f, totalSeconds - clip.DurationSeconds * 0.25f);
        float newStart = Mathf.Clamp(mouseT, 0f, maxStart);

        if (Event.current.type == EventType.MouseDrag)
        {
            if (!Mathf.Approximately(newStart, clip.StartSeconds))
            {
                Undo.RecordObject(clip.UndoTarget, "Move Timeline Clip");
                Undo.RecordObject(timeline, "Move Timeline Clip");
                StageTimelineVisualSchedule.ApplyClipStartSeconds(clip, newStart, fps);
                EditorUtility.SetDirty(clip.UndoTarget);
                EditorUtility.SetDirty(timeline);
                viewer.OnEmbeddedConfigChanged();
                s_clips[_dragClipListIndex] = new StageTimelineVisualSchedule.Clip(
                    clip.Kind,
                    clip.Index,
                    clip.Label,
                    newStart,
                    clip.DurationSeconds,
                    clip.UndoTarget);
            }

            Event.current.Use();
            return;
        }

        if (Event.current.type == EventType.MouseUp)
        {
            _dragClipListIndex = -1;
            Event.current.Use();
        }
    }

    static void SelectClip(StageTimelineConfigViewer viewer, StageTimelineVisualSchedule.Clip clip)
    {
        switch (clip.Kind)
        {
            case StageTimelineVisualSchedule.ClipKind.MidStageWave:
                viewer.SetPreviewMidStageWaveIndex(clip.Index);
                break;
            case StageTimelineVisualSchedule.ClipKind.MidBoss:
                viewer.SetPathEditTarget(E_StageTimelinePathEditTarget.MidBoss);
                break;
            case StageTimelineVisualSchedule.ClipKind.MainBoss:
                viewer.SetPathEditTarget(E_StageTimelinePathEditTarget.MainBoss);
                break;
        }
    }

    static bool IsClipSelected(StageTimelineConfigViewer viewer, StageTimelineVisualSchedule.Clip clip)
    {
        return clip.Kind switch
        {
            StageTimelineVisualSchedule.ClipKind.MidStageWave =>
                viewer.PathEditTarget == E_StageTimelinePathEditTarget.MidStageWave
                && viewer.PreviewMidStageWaveIndex == clip.Index,
            StageTimelineVisualSchedule.ClipKind.MidBoss =>
                viewer.PathEditTarget == E_StageTimelinePathEditTarget.MidBoss,
            StageTimelineVisualSchedule.ClipKind.MainBoss =>
                viewer.PathEditTarget == E_StageTimelinePathEditTarget.MainBoss,
            _ => false,
        };
    }
}
#endif
