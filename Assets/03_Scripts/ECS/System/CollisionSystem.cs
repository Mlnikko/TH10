using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class DeterministicGrid
{
    private readonly int _cellSize; // 统一正方形格子
    private readonly float _worldMinX, _worldMinY;
    private readonly int _gridWidth, _gridHeight;
    private readonly List<int>[] _cells;

    public DeterministicGrid(BattleAreaConfig config)
    {
        _cellSize = config.GridCellSize; // 来自配置

        float marginX = config.DanmakuRecycleMargin.x;
        float marginY = config.DanmakuRecycleMargin.y;

        // 总覆盖范围 = 战斗区域 + 两侧回收边界
        float totalWidth = config.Width + marginX * 2f;
        float totalHeight = config.Height + marginY * 2f;

        // 向上取整，确保覆盖全部
        _gridWidth = Mathf.CeilToInt(totalWidth / _cellSize);
        _gridHeight = Mathf.CeilToInt(totalHeight / _cellSize);

        // 网格世界原点：战斗区域左下角 - 回收边界
        float battleLeft = config.Center.x - config.Width * 0.5f;
        float battleBottom = config.Center.y - config.Height * 0.5f;
        _worldMinX = battleLeft - marginX;
        _worldMinY = battleBottom - marginY;

        // 初始化网格
        int totalCells = _gridWidth * _gridHeight;
        _cells = new List<int>[totalCells];
        for (int i = 0; i < totalCells; i++)
            _cells[i] = new List<int>();
    }

    public void Clear()
    {
        foreach (var cell in _cells)
            cell.Clear();
    }

    public void Insert(int entity, Vector2 worldPos, in CCollider col)
    {
        GetBounds(worldPos, col, out Vector2 min, out Vector2 max);

        int minCellX = WorldToCellX(min.x);
        int maxCellX = WorldToCellX(max.x);
        int minCellY = WorldToCellY(min.y);
        int maxCellY = WorldToCellY(max.y);

        for (int x = minCellX; x <= maxCellX; x++)
        {
            if (x < 0 || x >= _gridWidth) continue;
            for (int y = minCellY; y <= maxCellY; y++)
            {
                if (y < 0 || y >= _gridHeight) continue;
                _cells[y * _gridWidth + x].Add(entity);
            }
        }
    }

    public int Query(Vector2 worldPos, in CCollider col, int[] outputBuffer, BitSet tempSet)
    {
        GetBounds(worldPos, col, out Vector2 min, out Vector2 max);

        int minCellX = WorldToCellX(min.x);
        int maxCellX = WorldToCellX(max.x);
        int minCellY = WorldToCellY(min.y);
        int maxCellY = WorldToCellY(max.y);

        tempSet.ClearAll();

        for (int x = minCellX; x <= maxCellX; x++)
        {
            if (x < 0 || x >= _gridWidth) continue;
            for (int y = minCellY; y <= maxCellY; y++)
            {
                if (y < 0 || y >= _gridHeight) continue;
                int idx = y * _gridWidth + x;
                foreach (int e in _cells[idx])
                    tempSet.Set(e, true);
            }
        }

        return tempSet.GetSetBits(outputBuffer); // 返回升序 ID
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WorldToCellX(float x) => (int)((x - _worldMinX) / _cellSize);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WorldToCellY(float y) => (int)((y - _worldMinY) / _cellSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetBounds(Vector2 center, in CCollider col, out Vector2 min, out Vector2 max)
    {
        if (col.Type == E_ColliderType.Circle)
        {
            min = new Vector2(center.x - col.Radius, center.y - col.Radius);
            max = new Vector2(center.x + col.Radius, center.y + col.Radius);
        }
        else // Rect
        {
            float halfW = col.Width * 0.5f;
            float halfH = col.Height * 0.5f;
            min = new Vector2(center.x - halfW, center.y - halfH);
            max = new Vector2(center.x + halfW, center.y + halfH);
        }
    }
}

public class CollisionSystem : BaseSystem
{
    private BattleAreaConfig _battleConfig; // ← 类型变更
    private DeterministicGrid _grid;

    // 缓冲区（避免 GC）
    private readonly int[] _activeColliders = new int[EntityManager.MAX_ENTITIES];
    private readonly int[] _queryResults = new int[EntityManager.MAX_ENTITIES];
    private readonly BitSet _tempBitSet = new BitSet(EntityManager.MAX_ENTITIES);

    public void Initialize(BattleAreaConfig config)
    {
        _battleConfig = config;
        _grid = new DeterministicGrid(config); // ← 传入新配置
    }

    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        if (!Enabled) return;

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var colliders = EntityManager.GetComponentSpan<CCollider>();
        var active = EntityManager.ActiveEntities;

        // Step 1: 收集所有活跃且启用的碰撞体（按 Entity ID 升序）
        int colliderCount = 0;
        for (int i = 0; i < EntityManager.MAX_ENTITIES; i++)
        {
            if (active[i] && colliders[i].Active)
                _activeColliders[colliderCount++] = i;
        }

        // Step 2: 清空并重建网格
        _grid.Clear();
        for (int idx = 0; idx < colliderCount; idx++)
        {
            int e = _activeColliders[idx];
            ref readonly var col = ref colliders[e];
            Vector2 worldPos = new Vector2(positions[e].x, positions[e].y);
            _grid.Insert(e, worldPos, col);
        }

        // Step 3: 检测碰撞（顺序严格确定：i < j）
        for (int iIdx = 0; iIdx < colliderCount; iIdx++)
        {
            int i = _activeColliders[iIdx];
            ref readonly var colA = ref colliders[i];
            Vector2 posA = new Vector2(positions[i].x, positions[i].y);

            // 查询潜在碰撞对象（返回升序 ID 列表）
            int queryCount = _grid.Query(posA, colA, _queryResults, _tempBitSet);

            for (int k = 0; k < queryCount; k++)
            {
                int j = _queryResults[k];
                if (j <= i) continue; // 避免重复 & 保证顺序

                ref readonly var colB = ref colliders[j];
                Vector2 posB = new Vector2(positions[j].x, positions[j].y);

                // 层级过滤（双向）
                if ((colA.Mask & (1 << colB.Layer)) == 0) continue;
                if ((colB.Mask & (1 << colA.Layer)) == 0) continue;

                if (CheckCollision(in colA, posA, in colB, posB))
                {
                    HandleCollision(i, j); // 你的响应逻辑（发事件、扣血等）
                }
            }
        }
    }

    // 用户可重写
    protected void HandleCollision(int entityA, int entityB)
    {

    }

    #region 碰撞检测（确定性版本）

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCollision(
        in CCollider a, Vector2 worldPosA,
        in CCollider b, Vector2 worldPosB)
    {
        return (a.Type, b.Type) switch
        {
            (E_ColliderType.Circle, E_ColliderType.Circle) => CheckCircleCircle(worldPosA, a.Radius, worldPosB, b.Radius),
            (E_ColliderType.Circle, E_ColliderType.Rect) => CheckCircleRect(worldPosA, a.Radius, worldPosB, b.Width, b.Height),
            (E_ColliderType.Rect, E_ColliderType.Circle) => CheckCircleRect(worldPosB, b.Radius, worldPosA, a.Width, a.Height),
            (E_ColliderType.Rect, E_ColliderType.Rect) => CheckRectRect(worldPosA, a.Width, a.Height, worldPosB, b.Width, b.Height),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCircleCircle(Vector2 a, float rA, Vector2 b, float rB)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        float distSq = dx * dx + dy * dy;
        float rSum = rA + rB;
        return distSq <= rSum * rSum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCircleRect(Vector2 circle, float radius, Vector2 rect, float w, float h)
    {
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;

        float clampedX = circle.x < rect.x - halfW ? rect.x - halfW :
                         circle.x > rect.x + halfW ? rect.x + halfW : circle.x;
        float clampedY = circle.y < rect.y - halfH ? rect.y - halfH :
                         circle.y > rect.y + halfH ? rect.y + halfH : circle.y;

        float dx = circle.x - clampedX;
        float dy = circle.y - clampedY;
        float distSq = dx * dx + dy * dy;
        return distSq <= radius * radius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckRectRect(Vector2 a, float aW, float aH, Vector2 b, float bW, float bH)
    {
        float aLeft = a.x - aW * 0.5f;
        float aRight = a.x + aW * 0.5f;
        float aBottom = a.y - aH * 0.5f;
        float aTop = a.y + aH * 0.5f;

        float bLeft = b.x - bW * 0.5f;
        float bRight = b.x + bW * 0.5f;
        float bBottom = b.y - bH * 0.5f;
        float bTop = b.y + bH * 0.5f;

        return aLeft <= bRight && aRight >= bLeft &&
               aBottom <= bTop && aTop >= bBottom;
    }

    #endregion
}