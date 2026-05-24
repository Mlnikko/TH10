using System;
using UnityEngine;

/// <summary>
/// 战斗区域数据
/// </summary>
[Serializable]
public struct BattleAreaData
{
    // === 战斗区域 ===
    public float Width;
    public float Height;
    public Vector2 Center;

    // === 网格加速参数 ===
    [Min(1)]
    public int GridCellSize;

    // === 弹幕回收边界（外扩区域）===
    [Min(0)]
    public Vector2 DanmakuRecycleMargin;

    public float Left => Center.x - Width * 0.5f;
    public float Right => Center.x + Width * 0.5f;
    public float Bottom => Center.y - Height * 0.5f;
    public float Top => Center.y + Height * 0.5f;

    public Rect BattleRect => new Rect(Left, Bottom, Width, Height);

    public float RecycleLeft => Left - DanmakuRecycleMargin.x;
    public float RecycleRight => Right + DanmakuRecycleMargin.x;
    public float RecycleBottom => Bottom - DanmakuRecycleMargin.y;
    public float RecycleTop => Top + DanmakuRecycleMargin.y;

    public Vector2 GridWorldOrigin => new Vector2(
        RecycleLeft - 50f,
        RecycleBottom - 50f
    );

    public float TotalWidth => Width + DanmakuRecycleMargin.x * 2f + 100f;
    public float TotalHeight => Height + DanmakuRecycleMargin.y * 2f + 100f;

    public int GridColumns => Mathf.CeilToInt(TotalWidth / GridCellSize);
    public int GridRows => Mathf.CeilToInt(TotalHeight / GridCellSize);

    public BattleAreaData(float width, float height, Vector2 center, int cellSize = 64, Vector2 recycleMargin = default)
    {
        Width = width;
        Height = height;
        Center = center;
        GridCellSize = cellSize;
        DanmakuRecycleMargin = recycleMargin == default ? new Vector2(100, 100) : recycleMargin;
    }

    public static BattleAreaData Default => new BattleAreaData(1280, 720, Vector2.zero, 64, new Vector2(100, 100));

    public bool IsPointInRecycleArea(float x, float y)
    {
        return x >= RecycleLeft && x <= RecycleRight && y >= RecycleBottom && y <= RecycleTop;
    }
}
