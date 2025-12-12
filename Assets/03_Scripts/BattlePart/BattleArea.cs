using System;
using UnityEngine;

/// <summary>
/// 战斗区域配置
/// </summary>
[Serializable]
public struct BattleAreaData : IEquatable<BattleAreaData>
{
    // === 战斗区域 ===
    public float Width;          // 战斗区域宽度（单位：Unity 世界单位）
    public float Height;         // 战斗区域高度
    public Vector2 Center;       // 战斗区域中心点（通常为 (0,0)）

    // === 网格加速参数 ===
    public int GridCellSize;     // 网格单元格尺寸（建议 64 或 128，正方形）

    // === 弹幕回收边界（外扩区域）===
    public Vector2 DanmakuRecycleMargin; // 超出战斗区域多少距离后回收弹幕

    // === 辅助属性（只读，计算得来）===

    public float Left => Center.x - Width * 0.5f;
    public float Right => Center.x + Width * 0.5f;
    public float Bottom => Center.y - Height * 0.5f;
    public float Top => Center.y + Height * 0.5f;

    public Rect BattleRect => new Rect(Left, Bottom, Width, Height);

    // 回收区域边界（用于销毁远距离弹幕）
    public float RecycleLeft => Left - DanmakuRecycleMargin.x;
    public float RecycleRight => Right + DanmakuRecycleMargin.x;
    public float RecycleBottom => Bottom - DanmakuRecycleMargin.y;
    public float RecycleTop => Top + DanmakuRecycleMargin.y;

    // 用于 DeterministicGrid 的世界原点（左下角 - 边距）
    public Vector2 GridWorldOrigin => new Vector2(
        RecycleLeft - 50f,   // 额外安全边距
        RecycleBottom - 50f
    );

    // 总覆盖宽度/高度（用于计算网格大小）
    public float TotalWidth => Width + DanmakuRecycleMargin.x * 2f + 100f;
    public float TotalHeight => Height + DanmakuRecycleMargin.y * 2f + 100f;

    // 网格维度（向上取整）
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

    // 默认构造（避免未初始化）
    public static BattleAreaData Default => new BattleAreaData(1280, 720, Vector2.zero, 64, new Vector2(100, 100));

    // 用于帧同步一致性校验
    public bool Equals(BattleAreaData other)
    {
        return Width == other.Width &&
               Height == other.Height &&
               Center.Equals(other.Center) &&
               GridCellSize == other.GridCellSize &&
               DanmakuRecycleMargin.Equals(other.DanmakuRecycleMargin);
    }

    public override bool Equals(object obj) => obj is BattleAreaData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Width, Height, Center, GridCellSize, DanmakuRecycleMargin);
}

public class BattleArea : MonoBehaviour
{
    [Header("运行时生成的配置（只读）")]
    [SerializeField] BattleAreaData battleAreaData = new();

    [Header("编辑器参数")]
    [Tooltip("弹幕回收外扩距离（世界单位，建议 50~200）")]
    public Vector2 danmakuRecycleBounds = new(0.5f, 0.5f);

    [Tooltip("用于 DeterministicGrid 的单元格尺寸")]
    [Min(1)] public int gridCellSize = 64;

    [Header("可视化")]
    [SerializeField] private bool showGrid = true;

    /// <summary>
    /// 从当前 Transform 自动生成 BattleAreaData
    /// 调用后 battleAreaData 即可用于其他系统
    /// </summary>
    public BattleAreaData InitBattleArea()
    {
        Vector3 size = transform.localScale;
        Vector3 center = transform.position;

        battleAreaData.Width = size.x;
        battleAreaData.Height = size.y;
        battleAreaData.Center = new Vector2(center.x, center.y);
        battleAreaData.DanmakuRecycleMargin = danmakuRecycleBounds;
        battleAreaData.GridCellSize = gridCellSize;

        return battleAreaData;
    }

    void OnDrawGizmos()
    {
        // 临时计算当前配置（即使未 Awake 也能预览）
        Vector3 size = transform.localScale;
        Vector3 center = transform.position;
        Rect battleRect = new Rect(
            center.x - size.x * 0.5f,
            center.y - size.y * 0.5f,
            size.x,
            size.y
        );

        // 绘制弹幕回收边界（红色）
        Gizmos.color = Color.red;
        Vector3 recycleSize = new Vector3(
            size.x + danmakuRecycleBounds.x * 2f,
            size.y + danmakuRecycleBounds.y * 2f,
            0
        );
        Gizmos.DrawWireCube(center, recycleSize);

        // 绘制战斗区域（绿色）
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0));

        // 绘制网格（浅绿）
        if (showGrid && gridCellSize > 0)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
            float cellSize = gridCellSize;

            // 计算覆盖整个战斗区域的网格范围
            float left = battleRect.xMin;
            float right = battleRect.xMax;
            float bottom = battleRect.yMin;
            float top = battleRect.yMax;

            // 垂直线
            for (float x = left; x <= right + 0.1f; x += cellSize)
            {
                Gizmos.DrawLine(new Vector3(x, bottom), new Vector3(x, top));
            }
            // 水平线
            for (float y = bottom; y <= top + 0.1f; y += cellSize)
            {
                Gizmos.DrawLine(new Vector3(left, y), new Vector3(right, y));
            }
        }
    }
}
