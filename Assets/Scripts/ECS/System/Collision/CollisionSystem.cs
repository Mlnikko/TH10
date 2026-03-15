using System;
using System.Runtime.CompilerServices;

public class CollisionSystem : BaseSystem
{
    DeterministicGrid _grid;

    protected override void OnCreate()
    {
        _grid = new DeterministicGrid(GlobalBattleData.AreaData);
    }

    public override void OnLogicTick(uint currentframe)
    {
        if (_grid == null) return;
        Span<int> activeColliders = TempBuffers.CollisionActive;
        Span<int> queryResults = TempBuffers.CollisionQuery;

        Span<int> indices = ComponentStorage<CCollider>.GetActiveIndices();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var colliders = EntityManager.GetComponentSpan<CCollider>();

        // Step 1: 收集所有活跃且启用的碰撞体（按 Entity ID 升序）
        int colliderCount = 0;
        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            if (colliders[idx].isActive)
                activeColliders[colliderCount++] = idx;
        }

        // Step 2: 清空并重建网格
        _grid.Clear();

        for (int idx = 0; idx < colliderCount; idx++)
        {
            int e = activeColliders[idx];
            ref readonly var col = ref colliders[e];

            // 插入时使用带偏移的碰撞中心
            float cx = positions[e].x + col.offsetX;
            float cy = positions[e].y + col.offsetY;
            _grid.Insert(e, cx, cy, col);
        }

        // Step 3: 检测碰撞（顺序严格确定：i < j）
        for (int iIdx = 0; iIdx < colliderCount; iIdx++)
        {
            int i = activeColliders[iIdx];
            ref readonly var colA = ref colliders[i];

            // 查询也使用带偏移的位置
            float ax = positions[i].x + colA.offsetX;
            float ay = positions[i].y + colA.offsetY;

            int queryCount = _grid.Query(ax, ay, colA, queryResults, TempBitSets.Collision);

            for (int k = 0; k < queryCount; k++)
            {
                int j = queryResults[k];
                if (j <= i) continue;

                ref readonly var colB = ref colliders[j];
                float bx = positions[j].x + colB.offsetX;
                float by = positions[j].y + colB.offsetY;

                // 层级过滤（双向）
                if ((colA.mask & colB.layer) == 0) continue;
                if ((colB.mask & colA.layer) == 0) continue;

                if (TryGetCollisionInfo(colA, ax, ay, colB, bx, by, out float contactX, out float contactY))
                {
                    // 【修复点】直接使用局部变量，不再依赖不存在的 pair/contact 对象
                    var evt = new CollisionEvent
                    {
                        EntityA = EntityManager.GetEntity(i), // 使用索引或 EntityManager.GetID(i) 取决于你的 Event 定义
                        EntityB = EntityManager.GetEntity(j),
                        ContactX = contactX,
                        ContactY = contactY,
#if UNITY_EDITOR
                        Frame = currentframe // 帧号用于调试
#endif
                    };

                    CollisionEventBuffer.Add(evt);
                }
            }
        }
    }

    #region 碰撞检测与信息提取 (确定性版本)

    /// <summary>
    /// 检测碰撞并返回接触点。如果未碰撞，返回 false。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryGetCollisionInfo(in CCollider colA, float aX, float aY, in CCollider colB, float bX, float bY,
        out float contactX, out float contactY)
    {
        contactX = 0f;
        contactY = 0f;

        return (colA.type, colB.type) switch
        {
            (E_ColliderShape.Circle, E_ColliderShape.Circle) =>
                CheckCircleCircleInfo(aX, aY, colA.radius, bX, bY, colB.radius, out contactX, out contactY),

            (E_ColliderShape.Circle, E_ColliderShape.Rect) =>
                CheckCircleRectInfo(aX, aY, colA.radius, bX, bY, colB.width, colB.height, out contactX, out contactY),

            (E_ColliderShape.Rect, E_ColliderShape.Circle) =>
                CheckCircleRectInfo(bX, bY, colB.radius, aX, aY, colA.width, colA.height, out contactX, out contactY),

            (E_ColliderShape.Rect, E_ColliderShape.Rect) =>
                CheckRectRectInfo(aX, aY, colA.width, colA.height, bX, bY, colB.width, colB.height, out contactX, out contactY),

            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCircleCircleInfo(float aX, float aY, float aR, float bX, float bY, float bR,
        out float contactX, out float contactY)
    {
        float dx = bX - aX;
        float dy = bY - aY;
        float distSq = dx * dx + dy * dy;
        float rSum = aR + bR;

        // 1. 快速排除：距离太远
        if (distSq > rSum * rSum)
        {
            contactX = contactY = 0f;
            return false;
        }

        // 2. 处理重合或极近距离 (避免除零，并定义接触点)
        if (distSq == 0f)
        {
            // 圆心重合：接触点定义为两者的中心（其实就是 aX, aY）
            // 这样特效会在重合中心爆炸，视觉效果合理
            contactX = aX;
            contactY = aY;
            return true;
        }

        // 3. 正常碰撞：计算精确接触点
        float dist = MathF.Sqrt(distSq);

        // 接触点公式：A 圆心 + (A 半径 / 总距离) * (B - A)
        // 这个点既在 A 的圆周上，也在 B 的圆周上
        float ratio = aR / dist;
        contactX = aX + dx * ratio;
        contactY = aY + dy * ratio;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCircleRectInfo(float cX, float cY, float radius, float rX, float rY, float w, float h,
        out float contactX, out float contactY)
    {
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;

        // 找到矩形上离圆心最近的点 (Clamped Point)
        float closestX = MathF.Max(rX - halfW, MathF.Min(cX, rX + halfW));
        float closestY = MathF.Max(rY - halfH, MathF.Min(cY, rY + halfH));

        float dx = cX - closestX;
        float dy = cY - closestY;
        float distSq = dx * dx + dy * dy;

        if (distSq > radius * radius)
        {
            contactX = contactY = 0f;
            return false;
        }

        if (distSq == 0f)
        {
            contactX = rX;
            contactY = rY;
            return true;
        }

        contactX = closestX;
        contactY = closestY;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckRectRectInfo(float aX, float aY, float aW, float aH, float bX, float bY, float bW, float bH,
        out float contactX, out float contactY)
    {
        float aLeft = aX - aW * 0.5f;
        float aRight = aX + aW * 0.5f;
        float aBottom = aY - aH * 0.5f;
        float aTop = aY + aH * 0.5f;

        float bLeft = bX - bW * 0.5f;
        float bRight = bX + bW * 0.5f;
        float bBottom = bY - bH * 0.5f;
        float bTop = bY + bH * 0.5f;

        // 快速排斥
        if (aLeft > bRight || aRight < bLeft || aBottom > bTop || aTop < bBottom)
        {
            contactX = contactY = 0f;
            return false;
        }

        // 计算重叠区域中心作为接触点 (简单近似，适用于 STG)
        float overlapLeft = MathF.Max(aLeft, bLeft);
        float overlapRight = MathF.Min(aRight, bRight);
        float overlapBottom = MathF.Max(aBottom, bBottom);
        float overlapTop = MathF.Min(aTop, bTop);

        contactX = (overlapLeft + overlapRight) * 0.5f;
        contactY = (overlapBottom + overlapTop) * 0.5f;
        return true;
    }

    #endregion
}