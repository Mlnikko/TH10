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

    // === 网格加速参数（Scene Gizmo 与 DeterministicGrid 共用）===
    [Min(0.01f)]
    [Tooltip("单格边长（世界单位）。战斗区较小时可用 0.1～0.5；例如 4×5 区域设 0.25 约 16×20 格")]
    public float GridCellSize;

    // === GO回收边界（外扩区域）===
    [Min(0)]
    public Vector2 GO_RecycleMargin;

    public float Left => Center.x - Width * 0.5f;
    public float Right => Center.x + Width * 0.5f;
    public float Bottom => Center.y - Height * 0.5f;
    public float Top => Center.y + Height * 0.5f;

    public Rect BattleRect => new Rect(Left, Bottom, Width, Height);

    public float RecycleLeft => Left - GO_RecycleMargin.x;
    public float RecycleRight => Right + GO_RecycleMargin.x;
    public float RecycleBottom => Bottom - GO_RecycleMargin.y;
    public float RecycleTop => Top + GO_RecycleMargin.y;

    /// <summary>碰撞网格左下角，与 <see cref="BattleRect"/> 左下角对齐。</summary>
    public Vector2 GridWorldOrigin => new Vector2(Left, Bottom);

    /// <summary>覆盖战斗区的列数（向上取整，至少 1）。</summary>
    public int GridColumns => Mathf.Max(1, Mathf.CeilToInt(Width / GridCellSize));

    /// <summary>覆盖战斗区的行数（向上取整，至少 1）。</summary>
    public int GridRows => Mathf.Max(1, Mathf.CeilToInt(Height / GridCellSize));

    /// <summary>网格世界宽度（= GridColumns × GridCellSize，略大于等于 Width）。</summary>
    public float TotalWidth => GridColumns * GridCellSize;

    /// <summary>网格世界高度（= GridRows × GridCellSize，略大于等于 Height）。</summary>
    public float TotalHeight => GridRows * GridCellSize;

    public float GridMaxX => GridWorldOrigin.x + TotalWidth;
    public float GridMaxY => GridWorldOrigin.y + TotalHeight;

    public BattleAreaData(float width, float height, Vector2 center, float cellSize = 64f, Vector2 recycleMargin = default)
    {
        Width = width;
        Height = height;
        Center = center;
        GridCellSize = cellSize;
        GO_RecycleMargin = recycleMargin == default ? new Vector2(100, 100) : recycleMargin;
    }

    public static BattleAreaData Default => new BattleAreaData(1280, 720, Vector2.zero, 64f, new Vector2(100, 100));

    public bool IsPointInRecycleArea(float x, float y)
    {
        return x >= RecycleLeft && x <= RecycleRight && y >= RecycleBottom && y <= RecycleTop;
    }

    /// <summary>点是否在战斗区内（含边界）。</summary>
    public bool IsPointInBattleArea(float x, float y)
    {
        return x >= Left && x <= Right && y >= Bottom && y <= Top;
    }

    /// <summary>在 GO 回收区内且不在战斗区内（刷怪合法带）。</summary>
    public bool IsPointInExteriorRecycleBand(float x, float y)
    {
        return IsPointInRecycleArea(x, y) && !IsPointInBattleArea(x, y);
    }
}
