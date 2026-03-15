using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class DeterministicGrid
{
    readonly int _cellSize; // 格子边长（像素或单位）
    readonly float _worldMinX, _worldMinY;
    readonly int _gridWidth, _gridHeight;
    readonly List<int>[] _cells;

    public DeterministicGrid(BattleAreaData battleAreaData)
    {
        _cellSize = battleAreaData.GridCellSize;

        // 直接使用 BattleAreaData 提供的“总尺寸”和“世界原点”
        _gridWidth = battleAreaData.GridColumns;   // 已含安全边距 + 向上取整
        _gridHeight = battleAreaData.GridRows;

        _worldMinX = battleAreaData.GridWorldOrigin.x;
        _worldMinY = battleAreaData.GridWorldOrigin.y;

        int totalCells = _gridWidth * _gridHeight;
        _cells = new List<int>[totalCells];
        for (int i = 0; i < totalCells; i++)
            _cells[i] = new List<int>();
    }

    /// <summary>
    /// 清空整个网格（全量重建时使用）
    /// </summary>
    public void Clear()
    {
        foreach (var cell in _cells)
            cell.Clear();
    }

    /// <summary>
    /// 将实体插入到所有覆盖的格子中
    /// </summary>
    /// <param name="entity">实体ID</param>
    /// <param name="cx">碰撞体中心世界X（= position.x + collider.offsetX）</param>
    /// <param name="cy">碰撞体中心世界Y（= position.y + collider.offsetY）</param>
    /// <param name="col">碰撞体数据</param>
    public void Insert(int entity, float cx, float cy, in CCollider col)
    {
        GetBounds(cx, cy, col, out float minX, out float minY, out float maxX, out float maxY);

        int minCellX = WorldToCellX(minX);
        int maxCellX = WorldToCellX(maxX);
        int minCellY = WorldToCellY(minY);
        int maxCellY = WorldToCellY(maxY);

        for (int x = minCellX; x <= maxCellX; x++)
        {
            if ((uint)x >= (uint)_gridWidth) continue; // 快速无分支越界检查
            for (int y = minCellY; y <= maxCellY; y++)
            {
                if ((uint)y >= (uint)_gridHeight) continue;
                _cells[y * _gridWidth + x].Add(entity);
            }
        }
    }

    /// <summary>
    /// 查询与指定碰撞体可能相交的所有实体（去重、升序）
    /// </summary>
    public int Query(float cx, float cy, in CCollider col, Span<int> outputBuffer, BitSet tempSet)
    {
        GetBounds(cx, cy, col, out float minX, out float minY, out float maxX, out float maxY);

        int minCellX = WorldToCellX(minX);
        int maxCellX = WorldToCellX(maxX);
        int minCellY = WorldToCellY(minY);
        int maxCellY = WorldToCellY(maxY);

        tempSet.ClearAll();

        for (int x = minCellX; x <= maxCellX; x++)
        {
            if ((uint)x >= (uint)_gridWidth) continue;
            for (int y = minCellY; y <= maxCellY; y++)
            {
                if ((uint)y >= (uint)_gridHeight) continue;
                int idx = y * _gridWidth + x;
                foreach (int e in _cells[idx])
                    tempSet.Set(e, true);
            }
        }

        return tempSet.GetSetBits(outputBuffer); // 返回升序 Entity ID
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int WorldToCellX(float x) => (int)MathF.Floor((x - _worldMinX) / _cellSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int WorldToCellY(float y) => (int)MathF.Floor((y - _worldMinY) / _cellSize);

    /// <summary>
    /// 计算碰撞体的轴对齐包围盒（AABB）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void GetBounds(float cx, float cy, in CCollider col, out float minX, out float minY, out float maxX, out float maxY)
    {
        if (col.type == E_ColliderShape.Circle)
        {
            minX = cx - col.radius;
            minY = cy - col.radius;
            maxX = cx + col.radius;
            maxY = cy + col.radius;
        }
        else // Rect
        {
            float halfW = col.width * 0.5f;
            float halfH = col.height * 0.5f;
            minX = cx - halfW;
            minY = cy - halfH;
            maxX = cx + halfW;
            maxY = cy + halfH;
        }
    }
}
