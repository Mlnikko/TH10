using System.Collections.Generic;
using UnityEngine;

public class DanmakuArea : MonoBehaviour
{
    public static Rect battleRect;

    [Header("弹幕区域边界扩展")]
    public Vector2 boundsThickness;

    [Header("网格划分")]
    [SerializeField] bool showGrid = true;
    [SerializeField] int gridRows = 4;
    [SerializeField] int gridColumns = 4;

    public int GridRows => gridRows;
    public int GridColumns => gridColumns;

    void Awake()
    {
        InitBattleRect();
    }

    void InitBattleRect()
    {
        Vector3 size = transform.localScale;
        Vector3 center = transform.position;
        battleRect = new Rect(center.x - size.x / 2, center.y - size.y / 2, size.x, size.y);
    }

    /// <summary>
    /// 是否超出弹幕区域边界（包含扩展部分）
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool IsOutOfDanmakuRect(Vector2 pos)
    {
        if (pos.x > battleRect.xMax + boundsThickness.x) return true;
        if (pos.x < battleRect.xMin - boundsThickness.x) return true;
        if (pos.y > battleRect.yMax + boundsThickness.y) return true;
        if (pos.y < battleRect.yMin - boundsThickness.y) return true;
        return false;
    }

    /// <summary>
    /// 是否超出战斗区域边界（不包含扩展部分）
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool IsOutOfBattleRect(Vector2 pos)
    {
        if (pos.x > battleRect.xMax) return true;
        if (pos.x < battleRect.xMin) return true;
        if (pos.y > battleRect.yMax) return true;
        if (pos.y < battleRect.yMin) return true;
        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 danmakuBounds = transform.localScale + (Vector3)boundsThickness * 2;
        Gizmos.DrawWireCube(transform.position, danmakuBounds);

        if (!showGrid) return;
        Gizmos.color = Color.green;
        InitBattleRect();
        Vector2 gridSize = new(battleRect.width / gridColumns, battleRect.height / gridRows);
        Vector2 origin = new(battleRect.xMin, battleRect.yMin);
        for (int i = 0; i <= gridColumns; i++)
        {
            Vector2 start = new(origin.x + i * gridSize.x, origin.y);
            Vector2 end = new(origin.x + i * gridSize.x, origin.y + battleRect.height);
            Gizmos.DrawLine(start, end);
        }
        for (int j = 0; j <= gridRows; j++)
        {
            Vector2 start = new(origin.x, origin.y + j * gridSize.y);
            Vector2 end = new(origin.x + battleRect.width, origin.y + j * gridSize.y);
            Gizmos.DrawLine(start, end);
        }
    }
}
