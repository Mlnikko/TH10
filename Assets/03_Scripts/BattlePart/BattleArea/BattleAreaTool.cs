using System;
using UnityEngine;

/// <summary>
/// 战斗区域数据
/// </summary>
[Serializable]
public class BattleAreaData
{
    // === 战斗区域 ===
    public float Width;          // 战斗区域宽度（单位：Unity 世界单位）
    public float Height;         // 战斗区域高度
    public Vector2 Center;       // 战斗区域中心点（通常为 (0,0)）

    // === 网格加速参数 ===
    [Min(1)]
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

public class BattleAreaTool : MonoBehaviour
{
    [Header("绑定配置")]
    public BattleAreaConfig battleAreaConfig;
    [SerializeField] BattleAreaData battleAreaData;

    [SerializeField] bool showGrid = true;

    public void LoadBattleAreaData()
    {
        if(battleAreaConfig != null) battleAreaData = battleAreaConfig.battleAreaData;
    }

    public void SaveBattleAreaData()
    {
        if (battleAreaConfig != null)
        {
            battleAreaConfig.battleAreaData = battleAreaData;         
        }
    }

    void OnDrawGizmos()
    {
        if (battleAreaConfig == null) return;

        // 战斗区域（绿色）
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(battleAreaData.Center, new Vector3(battleAreaData.Width, battleAreaData.Height, 0));

        // 回收边界（红色）
        Gizmos.color = Color.red;
        Vector3 recycleSize = new Vector3(
            battleAreaData.Width + battleAreaData.DanmakuRecycleMargin.x * 2f,
            battleAreaData.Height + battleAreaData.DanmakuRecycleMargin.y * 2f,
            0
        );
        Gizmos.DrawWireCube(battleAreaData.Center, recycleSize);

        // 网格（浅绿）—— 覆盖完整网格区域（含安全边距）
        if (showGrid && battleAreaData.GridCellSize > 0)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.3f);
            float cell = battleAreaData.GridCellSize;

            Vector2 origin = battleAreaData.GridWorldOrigin;
            Vector2 max = origin + new Vector2(battleAreaData.TotalWidth, battleAreaData.TotalHeight);

            for (float x = origin.x; x <= max.x; x += cell)
                Gizmos.DrawLine(new Vector3(x, origin.y), new Vector3(x, max.y));
            for (float y = origin.y; y <= max.y; y += cell)
                Gizmos.DrawLine(new Vector3(origin.x, y), new Vector3(max.x, y));
        }
    }
}
