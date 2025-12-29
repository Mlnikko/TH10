using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

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
    public int Query(float cx, float cy, in CCollider col, int[] outputBuffer, BitSet tempSet)
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
        if (col.type == E_ColliderType.Circle)
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

public class CollisionSystem : BaseSystem
{
    DeterministicGrid _grid;

    // 缓冲区
    readonly int[] _activeColliders = new int[EntityManager.MAX_ENTITIES];
    readonly int[] _queryResults = new int[EntityManager.MAX_ENTITIES];
    readonly BitSet _tempBitSet = new BitSet(EntityManager.MAX_ENTITIES);

    protected override void OnCreate()
    {
        base.OnCreate();
        //if(!BattleAreaTool.isInitialized)
        //{
        //    Logger.Error("BattleAreaTool 未初始化，无法正确创建网格！");
        //    return;
        //}
        //_grid = new DeterministicGrid(BattleAreaTool.battleAreaData);
    }

    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        if (_grid == null) return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var colliders = EntityManager.GetComponentSpan<CCollider>();

        // Step 1: 收集所有活跃且启用的碰撞体（按 Entity ID 升序）
        int colliderCount = 0;
        for (int i = 0; i < EntityManager.MAX_ENTITIES; i++)
        {
            if (EntityManager.IsValid(i) && colliders[i].active)
                _activeColliders[colliderCount++] = i;
        }

        // Step 2: 清空并重建网格
        _grid.Clear();

        for (int idx = 0; idx < colliderCount; idx++)
        {
            int e = _activeColliders[idx];
            ref readonly var col = ref colliders[e];

            // 插入时使用带偏移的碰撞中心
            float cx = positions[e].x + col.offsetX;
            float cy = positions[e].y + col.offsetY;
            _grid.Insert(e, cx, cy, col);
        }

        // Step 3: 检测碰撞（顺序严格确定：i < j）
        for (int iIdx = 0; iIdx < colliderCount; iIdx++)
        {
            int i = _activeColliders[iIdx];
            ref readonly var colA = ref colliders[i];

            // 查询也使用带偏移的位置
            float ax = positions[i].x + colA.offsetX;
            float ay = positions[i].y + colA.offsetY;

            int queryCount = _grid.Query(ax, ay, colA, _queryResults, _tempBitSet);

            for (int k = 0; k < queryCount; k++)
            {
                int j = _queryResults[k];
                if (j <= i) continue;

                ref readonly var colB = ref colliders[j];
                float bx = positions[j].x + colB.offsetX;
                float by = positions[j].y + colB.offsetY;

                // 层级过滤（双向）
                if ((colA.mask & colB.layer) == 0) continue;
                if ((colB.mask & colA.layer) == 0) continue;

                if (CheckCollision(colA, ax, ay, colB, bx, by))
                {
                    HandleCollision(i, j);
                }
            }
        }
    }

    // 用户可重写
    protected void HandleCollision(int entityA, int entityB)
    {
        Logger.Debug(entityA + " 与 " + entityB + " 发生碰撞！", LogTag.Collision);
    }

    #region 碰撞检测（确定性版本 - 纯 float）

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCollision(in CCollider colA, float ax, float ay, in CCollider colB, float bx, float by)
    {
        return (colA.type, colB.type) switch
        {
            (E_ColliderType.Circle, E_ColliderType.Circle) =>
                CheckCircleCircle(ax, ay, colA.radius, bx, by, colB.radius),

            (E_ColliderType.Circle, E_ColliderType.Rect) =>
                CheckCircleRect(ax, ay, colA.radius, bx, by, colB.width, colB.height),

            (E_ColliderType.Rect, E_ColliderType.Circle) =>
                CheckCircleRect(bx, by, colB.radius, ax, ay, colA.width, colA.height),

            (E_ColliderType.Rect, E_ColliderType.Rect) =>
                CheckRectRect(ax, ay, colA.width, colA.height, bx, by, colB.width, colB.height),

            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCircleCircle(float ax, float ay, float ar, float bx, float by, float br)
    {
        float dx = ax - bx;
        float dy = ay - by;
        float distSq = dx * dx + dy * dy;
        float rSum = ar + br;
        return distSq <= rSum * rSum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCircleRect(float cx, float cy, float radius, float rx, float ry, float w, float h)
    {
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;

        // 找到矩形上离圆心最近的点
        float clampedX = cx < rx - halfW ? rx - halfW :
                         cx > rx + halfW ? rx + halfW : cx;

        float clampedY = cy < ry - halfH ? ry - halfH :
                         cy > ry + halfH ? ry + halfH : cy;

        float dx = cx - clampedX;
        float dy = cy - clampedY;
        float distSq = dx * dx + dy * dy;
        return distSq <= radius * radius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckRectRect(float ax, float ay, float aW, float aH, float bx, float by, float bW, float bH)
    {
        float aLeft = ax - aW * 0.5f;
        float aRight = ax + aW * 0.5f;
        float aBottom = ay - aH * 0.5f;
        float aTop = ay + aH * 0.5f;

        float bLeft = bx - bW * 0.5f;
        float bRight = bx + bW * 0.5f;
        float bBottom = by - bH * 0.5f;
        float bTop = by + bH * 0.5f;

        return aLeft <= bRight && aRight >= bLeft && aBottom <= bTop && aTop >= bBottom;
    }

    #endregion
}