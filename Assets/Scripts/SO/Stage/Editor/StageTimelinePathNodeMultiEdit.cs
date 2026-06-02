#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Scene 路径点多选与批量拖拽（绑定 Viewer + 波次 + 队列条目）。</summary>
static class StageTimelinePathNodeMultiEdit
{
    const float PickDistanceThreshold = 12f;

    static int s_viewerId;
    static int s_ownerId;
    static int s_contextKey = -1;
    static readonly HashSet<int> s_selected = new();

    static readonly int s_centroidControlId = "TH10.PathNodeCentroid".GetHashCode();
    static readonly List<Vector2> s_centroidDragOriginLocal = new();
    static Vector3 s_centroidDragMouseWorld;

    public static void EnsureContext(StageTimelineConfigViewer viewer, UnityEngine.Object undoTarget, int contextKey)
    {
        int viewerId = viewer != null ? viewer.GetInstanceID() : 0;
        int ownerId = undoTarget != null ? undoTarget.GetInstanceID() : 0;
        if (viewerId == s_viewerId && ownerId == s_ownerId && contextKey == s_contextKey)
            return;

        s_viewerId = viewerId;
        s_ownerId = ownerId;
        s_contextKey = contextKey;
        ClearSelection();
    }

    public static bool IsSelected(int nodeIndex) => s_selected.Contains(nodeIndex);

    public static int SelectionCount => s_selected.Count;

    public static void ClearSelection() => s_selected.Clear();

    public static void SelectOnly(int nodeIndex)
    {
        s_selected.Clear();
        if (nodeIndex >= 0)
            s_selected.Add(nodeIndex);
    }

    public static void Toggle(int nodeIndex)
    {
        if (nodeIndex < 0)
            return;
        if (!s_selected.Remove(nodeIndex))
            s_selected.Add(nodeIndex);
    }

    public static void HandleInput(
        StageTimelineConfigViewer viewer,
        Vector2 spawn,
        in BattleAreaData area,
        IReadOnlyList<MovementPathNode> nodes,
        bool snap,
        float snapCell,
        ref bool changed)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            ClearSelection();
            e.Use();
            return;
        }

        if (GUIUtility.hotControl == s_centroidControlId)
            return;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && GUIUtility.hotControl == 0)
        {
            bool nearCentroid = SelectionCount >= 2
                                && TryGetCentroid(spawn, nodes, out Vector3 centroid)
                                && IsNearCentroidHandle(centroid);
            if (!nearCentroid)
            {
                int hit = HitTestNode(e.mousePosition, spawn, nodes, generousPick: true);
                if (hit >= 0)
                {
                    if (e.control)
                    {
                        Toggle(hit);
                        StageTimelinePathRouteSceneHandles.KeepViewerSelected(viewer);
                        e.Use();
                    }
                    else if (SelectionCount == 0)
                    {
                        // 无选区：各点可直接拖动；仅保持 Viewer 选中，不 Use 以免挡住 FreeMoveHandle
                        StageTimelinePathRouteSceneHandles.KeepViewerSelected(viewer);
                    }
                    else if (SelectionCount >= 2 && IsSelected(hit))
                    {
                        StageTimelinePathRouteSceneHandles.KeepViewerSelected(viewer);
                        e.Use();
                    }
                    else if (SelectionCount == 1 && IsSelected(hit))
                    {
                        StageTimelinePathRouteSceneHandles.KeepViewerSelected(viewer);
                    }
                    else
                    {
                        SelectOnly(hit);
                        StageTimelinePathRouteSceneHandles.KeepViewerSelected(viewer);
                        e.Use();
                    }
                }
                else if (e.control)
                {
                    ClearSelection();
                    e.Use();
                }
                else
                {
                    ClearSelection();
                }
            }
        }
    }

    public static void DrawNodes(
        StageTimelineConfigViewer viewer,
        UnityEngine.Object undoTarget,
        Vector2 spawn,
        in BattleAreaData area,
        PathRouteMovementData route,
        bool snap,
        float snapCell,
        ref bool changed)
    {
        var nodes = route.nodes;
        int last = nodes.Count - 1;
        int selCount = SelectionCount;
        bool multi = selCount >= 2;
        bool directMode = selCount == 0;

        if (multi)
            TryDrawCentroidMove(viewer, undoTarget, spawn, area, nodes, snap, snapCell, ref changed);

        for (int i = 0; i < nodes.Count; i++)
        {
            if (multi && IsSelected(i))
                continue;

            MovementPathNode node = nodes[i];
            Vector3 world = LocalToWorld(spawn, node.positionLocal);
            bool selected = IsSelected(i);
            bool draggable = directMode || (selected && selCount == 1);

            Handles.color = selected
                ? new Color(0.35f, 0.85f, 1f, 0.98f)
                : i == last ? EndColor : HoldColor;

            float handleSize = HandleUtility.GetHandleSize(world) * (draggable ? 0.12f : 0.1f);
            if (draggable)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.FreeMoveHandle(
                    world, handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(undoTarget, "Move Path Node");
                    ApplyWorldToNode(spawn, area, node, newWorld, snap, snapCell, ref changed);
                    if (directMode)
                        SelectOnly(i);
                }
            }
            else
            {
                Handles.SphereHandleCap(0, world, Quaternion.identity, handleSize, EventType.Repaint);
            }
        }

        if (selCount > 0)
        {
            DrawSelectionOutlines(spawn, nodes);
            DrawSelectionCoordinateLabels(spawn, nodes, last);
        }

        if (selCount > 0 && Event.current.type == EventType.Repaint)
            DrawSelectionModeHint(selCount, multi);
    }

    static void DrawSelectionModeHint(int selCount, bool multi)
    {
        Handles.BeginGUI();
        var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.85f, 0.95f, 1f) } };
        string hint = multi
            ? $"多选 {selCount} · 拖中心球移动 · Ctrl±选 · Esc 清空"
            : "已选路径点 · 显示局部/世界坐标 · Ctrl 多选";
        if (!string.IsNullOrEmpty(hint))
            GUI.Label(new Rect(8f, 8f, 360f, 20f), hint, style);
        Handles.EndGUI();
    }

    static bool TryDrawCentroidMove(
        StageTimelineConfigViewer viewer,
        UnityEngine.Object undoTarget,
        Vector2 spawn,
        in BattleAreaData area,
        List<MovementPathNode> nodes,
        bool snap,
        float snapCell,
        ref bool changed)
    {
        if (!TryGetCentroid(spawn, nodes, out Vector3 centroid))
            return false;

        float pickSize = GetCentroidPickSize(centroid);
        float drawSize = pickSize * 1.15f;
        Event e = Event.current;
        EventType et = e.GetTypeForControl(s_centroidControlId);

        if (et == EventType.Layout)
            HandleUtility.AddControl(s_centroidControlId, HandleUtility.DistanceToCircle(centroid, pickSize));

        Handles.color = new Color(0.35f, 0.85f, 1f, 0.98f);
        Handles.SphereHandleCap(s_centroidControlId, centroid, Quaternion.identity, drawSize, EventType.Repaint);
        Handles.DrawWireDisc(centroid, Vector3.forward, drawSize * 1.35f);
        Handles.Label(
            centroid + Vector3.up * (drawSize * 1.1f),
            $"移动 {SelectionCount} 点");

        switch (et)
        {
            case EventType.MouseDown:
                if (HandleUtility.nearestControl == s_centroidControlId && e.button == 0 && !e.alt)
                {
                    StageTimelinePathRouteSceneHandles.KeepViewerSelected(viewer);
                    GUIUtility.hotControl = s_centroidControlId;
                    s_centroidDragMouseWorld = MouseGuiToWorld(e.mousePosition);
                    CaptureCentroidDragOrigins(nodes);
                    Undo.RecordObject(undoTarget, "Move Path Nodes");
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == s_centroidControlId)
                {
                    Vector3 mouseWorld = MouseGuiToWorld(e.mousePosition);
                    Vector2 worldDelta = new Vector2(
                        mouseWorld.x - s_centroidDragMouseWorld.x,
                        mouseWorld.y - s_centroidDragMouseWorld.y);
                    ApplyCentroidWorldDelta(spawn, area, nodes, worldDelta, snap, snapCell, ref changed);
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == s_centroidControlId)
                {
                    GUIUtility.hotControl = 0;
                    s_centroidDragOriginLocal.Clear();
                    e.Use();
                }
                break;
        }

        DrawSelectionOutlines(spawn, nodes);
        return true;
    }

    static float GetCentroidPickSize(Vector3 centroid) =>
        Mathf.Max(HandleUtility.GetHandleSize(centroid) * 0.28f, 0.22f);

    static bool IsNearCentroidHandle(Vector3 centroid) =>
        HandleUtility.DistanceToCircle(centroid, GetCentroidPickSize(centroid)) < 24f;

    static Vector3 MouseGuiToWorld(Vector2 mouseGui)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mouseGui);
        if (Mathf.Abs(ray.direction.z) < 1e-5f)
            return ray.origin;

        float t = -ray.origin.z / ray.direction.z;
        return ray.GetPoint(t);
    }

    static void CaptureCentroidDragOrigins(IReadOnlyList<MovementPathNode> nodes)
    {
        s_centroidDragOriginLocal.Clear();
        foreach (int i in s_selected)
        {
            if (i >= 0 && i < nodes.Count)
                s_centroidDragOriginLocal.Add(nodes[i].positionLocal);
        }
    }

    static void ApplyCentroidWorldDelta(
        Vector2 spawn,
        in BattleAreaData area,
        IReadOnlyList<MovementPathNode> nodes,
        Vector2 worldDelta,
        bool snap,
        float snapCell,
        ref bool changed)
    {
        int k = 0;
        foreach (int i in s_selected)
        {
            if (i < 0 || i >= nodes.Count || k >= s_centroidDragOriginLocal.Count)
                continue;

            var node = nodes[i];
            var local = s_centroidDragOriginLocal[k] + worldDelta;
            if (snap)
                local = StageTimelinePathEditSnap.SnapLocal(local, spawn, area, snapCell);

            if (node.positionLocal != local)
            {
                node.positionLocal = local;
                changed = true;
            }

            k++;
        }
    }

    static bool TryGetCentroid(Vector2 spawn, IReadOnlyList<MovementPathNode> nodes, out Vector3 centroid)
    {
        centroid = default;
        if (s_selected.Count == 0)
            return false;

        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (int i in s_selected)
        {
            if (i < 0 || i >= nodes.Count)
                continue;
            sum += LocalToWorld(spawn, nodes[i].positionLocal);
            count++;
        }

        if (count == 0)
            return false;

        centroid = sum / count;
        return true;
    }

    static void DrawSelectionCoordinateLabels(Vector2 spawn, IReadOnlyList<MovementPathNode> nodes, int lastIndex)
    {
        if (s_selected.Count == 0 || Event.current.type != EventType.Repaint)
            return;

        Handles.color = new Color(0.85f, 0.95f, 1f, 0.95f);
        foreach (int i in s_selected)
        {
            if (i < 0 || i >= nodes.Count)
                continue;

            MovementPathNode node = nodes[i];
            Vector2 local = node.positionLocal;
            Vector3 world = LocalToWorld(spawn, local);
            float handleSize = HandleUtility.GetHandleSize(world);

            string role = i == lastIndex ? "终" : $"P{i + 1}";
            string text = $"{role}\n局部 ({local.x:F2}, {local.y:F2})\n世界 ({world.x:F2}, {world.y:F2})";
            Handles.Label(world + Vector3.up * (handleSize * 0.42f), text, CoordinateLabelStyle);
        }
    }

    static GUIStyle CoordinateLabelStyle
    {
        get
        {
            if (s_coordinateLabelStyle == null)
            {
                s_coordinateLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.85f, 0.95f, 1f, 0.95f) }
                };
            }

            return s_coordinateLabelStyle;
        }
    }

    static GUIStyle s_coordinateLabelStyle;

    static void DrawSelectionOutlines(Vector2 spawn, IReadOnlyList<MovementPathNode> nodes)
    {
        if (s_selected.Count == 0)
            return;

        Handles.color = new Color(0.35f, 0.85f, 1f, 0.9f);
        foreach (int i in s_selected)
        {
            if (i < 0 || i >= nodes.Count)
                continue;
            Vector3 world = LocalToWorld(spawn, nodes[i].positionLocal);
            float r = HandleUtility.GetHandleSize(world) * 0.16f;
            Handles.DrawWireDisc(world, Vector3.forward, r);
        }
    }

    static int HitTestNode(
        Vector2 mouseGui,
        Vector2 spawn,
        IReadOnlyList<MovementPathNode> nodes,
        bool ignoreSelected = false,
        bool generousPick = false)
    {
        float best = float.MaxValue;
        int bestIndex = -1;
        float pickScale = generousPick ? 0.16f : 0.12f;
        float maxDist = generousPick ? 20f : PickDistanceThreshold;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (ignoreSelected && IsSelected(i))
                continue;

            Vector3 world = LocalToWorld(spawn, nodes[i].positionLocal);
            float pickSize = HandleUtility.GetHandleSize(world) * pickScale;
            float dist = HandleUtility.DistanceToCircle(world, pickSize);
            if (dist < best)
            {
                best = dist;
                bestIndex = i;
            }
        }

        return best <= maxDist ? bestIndex : -1;
    }

    static Vector3 LocalToWorld(Vector2 spawn, Vector2 local) =>
        new Vector3(spawn.x + local.x, spawn.y + local.y, 0f);

    static bool ApplyWorldToNode(
        Vector2 spawn,
        in BattleAreaData area,
        MovementPathNode node,
        Vector3 world,
        bool snap,
        float snapCell,
        ref bool changed)
    {
        var local = new Vector2(world.x - spawn.x, world.y - spawn.y);
        if (snap)
            local = StageTimelinePathEditSnap.SnapLocal(local, spawn, area, snapCell);
        if (node.positionLocal == local)
            return false;

        node.positionLocal = local;
        changed = true;
        return true;
    }

    static readonly Color EndColor = new(1f, 0.55f, 0.15f, 0.95f);
    static readonly Color HoldColor = new(0.25f, 0.95f, 0.55f, 0.95f);
}
#endif
