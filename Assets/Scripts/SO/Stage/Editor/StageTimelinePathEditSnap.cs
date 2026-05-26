#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>关卡路径节点 Scene 编辑：以战斗区中心为基准的世界网格吸附与参考线绘制。</summary>
static class StageTimelinePathEditSnap
{
    public static Vector2 SnapWorld(Vector2 world, in BattleAreaData area, float cellSize)
    {
        if (cellSize <= 0.0001f)
            return world;

        Vector2 origin = area.Center;
        return new Vector2(
            origin.x + Mathf.Round((world.x - origin.x) / cellSize) * cellSize,
            origin.y + Mathf.Round((world.y - origin.y) / cellSize) * cellSize);
    }

    public static Vector2 SnapLocal(Vector2 local, Vector2 spawnWorld, in BattleAreaData area, float cellSize)
    {
        Vector2 world = spawnWorld + local;
        world = SnapWorld(world, area, cellSize);
        return world - spawnWorld;
    }

    public static void DrawGridForBattleArea(in BattleAreaData area, float cellSize)
    {
        if (cellSize <= 0.0001f)
            return;

        float left = area.RecycleLeft;
        float right = area.RecycleRight;
        float bottom = area.RecycleBottom;
        float top = area.RecycleTop;
        Vector2 center = area.Center;

        Handles.color = new Color(1f, 1f, 1f, 0.07f);
        float startX = center.x + Mathf.Floor((left - center.x) / cellSize) * cellSize;
        float startY = center.y + Mathf.Floor((bottom - center.y) / cellSize) * cellSize;

        for (float x = startX; x <= right + cellSize * 0.001f; x += cellSize)
        {
            var a = new Vector3(x, bottom, 0f);
            var b = new Vector3(x, top, 0f);
            Handles.DrawLine(a, b);
        }

        for (float y = startY; y <= top + cellSize * 0.001f; y += cellSize)
        {
            var a = new Vector3(left, y, 0f);
            var b = new Vector3(right, y, 0f);
            Handles.DrawLine(a, b);
        }

        DrawCenterCrosshair(center, left, right, bottom, top, cellSize);
    }

    static void DrawCenterCrosshair(
        Vector2 center,
        float left,
        float right,
        float bottom,
        float top,
        float cellSize)
    {
        var center3 = new Vector3(center.x, center.y, 0f);

        Handles.color = new Color(0.35f, 0.9f, 1f, 0.55f);
        Handles.DrawLine(new Vector3(center.x, bottom, 0f), new Vector3(center.x, top, 0f));
        Handles.DrawLine(new Vector3(left, center.y, 0f), new Vector3(right, center.y, 0f));

        float arm = Mathf.Max(cellSize * 1.5f, HandleUtility.GetHandleSize(center3) * 0.35f);
        Handles.color = new Color(0.35f, 0.9f, 1f, 0.85f);
        Handles.DrawLine(
            new Vector3(center.x - arm, center.y, 0f),
            new Vector3(center.x + arm, center.y, 0f));
        Handles.DrawLine(
            new Vector3(center.x, center.y - arm, 0f),
            new Vector3(center.x, center.y + arm, 0f));

        Handles.color = new Color(0.35f, 0.9f, 1f, 0.9f);
        Handles.DrawSolidDisc(center3, Vector3.forward, HandleUtility.GetHandleSize(center3) * 0.04f);
    }
}
#endif
