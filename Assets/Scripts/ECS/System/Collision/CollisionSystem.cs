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
        CollisionEventBuffer.Clear();

        if (_grid == null) return;
        Span<int> activeColliders = TempBuffers.CollisionActive;
        Span<int> queryResults = TempBuffers.CollisionQuery;

        Span<int> indices = ComponentStorage<CCollider>.GetActiveIndices();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();
        var colliders = EntityManager.GetComponentSpan<CCollider>();

        // Step 1: 收集所有活跃且启用的碰撞体
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
            ref readonly var pos = ref positions[e];
            ref readonly var rot = ref rotations[e];

            // --- 修改点：处理度数转弧度 ---
            
            // 1. 将度数转换为弧度
            float angleRad = rot.angle * MathF.PI / 180f;
            
            // 2. 预计算 Sin 和 Cos
            float cos = MathF.Cos(angleRad);
            float sin = MathF.Sin(angleRad);

            // 3. 计算旋转后的偏移量
            // 假设 offsetX/Y 是相对于 pos 的局部坐标
            float rotatedOx = col.offsetX * cos - col.offsetY * sin;
            float rotatedOy = col.offsetX * sin + col.offsetY * cos;

            // 4. 最终的世界坐标
            float cx = pos.x + rotatedOx;
            float cy = pos.y + rotatedOy;

            _grid.Insert(e, cx, cy, col);
        }

        // Step 3: 检测碰撞
        for (int iIdx = 0; iIdx < colliderCount; iIdx++)
        {
            int i = activeColliders[iIdx];
            ref readonly var colA = ref colliders[i];

            // --- 修改点：查询时也使用同样的旋转逻辑 ---
            ref readonly var posA = ref positions[i];
            ref readonly var rotA = ref rotations[i];
            
            float angleARad = rotA.angle * MathF.PI / 180f;
            float cosA = MathF.Cos(angleARad);
            float sinA = MathF.Sin(angleARad);

            float ax = posA.x + (colA.offsetX * cosA - colA.offsetY * sinA);
            float ay = posA.y + (colA.offsetX * sinA + colA.offsetY * cosA);

            int queryCount = _grid.Query(ax, ay, colA, queryResults, TempBitSets.Collision);

            for (int k = 0; k < queryCount; k++)
            {
                int j = queryResults[k];
                if (j <= i) continue;

                ref readonly var colB = ref colliders[j];
                ref readonly var posB = ref positions[j];
                ref readonly var rotB = ref rotations[j];

                // --- 修改点：获取 B 的弧度 ---
                float angleBRad = rotB.angle * MathF.PI / 180f;
                float cosB = MathF.Cos(angleBRad);
                float sinB = MathF.Sin(angleBRad);

                float bx = posB.x + (colB.offsetX * cosB - colB.offsetY * sinB);
                float by = posB.y + (colB.offsetX * sinB + colB.offsetY * cosB);

                // 层级过滤
                if ((colA.mask & colB.layer) == 0) continue;
                if ((colB.mask & colA.layer) == 0) continue;

                // 传入弧度计算出的 cos/sin
                if (TryGetCollisionInfo(colA, ax, ay, cosA, sinA, colB, bx, by, cosB, sinB, out float contactX, out float contactY))
                {
                    var evt = new CollisionEvent
                    {
                        EntityA = EntityManager.GetEntity(i),
                        EntityB = EntityManager.GetEntity(j),
                        ContactX = contactX,
                        ContactY = contactY,
#if UNITY_EDITOR
                        Frame = currentframe
#endif
                    };

                    CollisionEventBuffer.Add(evt);
                    Logger.Info($"[Collision] Detected collision between Entity {evt.EntityA} and Entity {evt.EntityB} at ({evt.ContactX}, {evt.ContactY}) on frame {currentframe}", LogTag.Collision);
                }
            }
        }
    }

    #region 碰撞检测与信息提取 (支持旋转)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryGetCollisionInfo(in CCollider colA, float aX, float aY, float cosA, float sinA, 
                                    in CCollider colB, float bX, float bY, float cosB, float sinB,
                                    out float contactX, out float contactY)
    {
        contactX = 0f;
        contactY = 0f;

        return (colA.shape, colB.shape) switch
        {
            (E_ColliderShape.Circle, E_ColliderShape.Circle) =>
                CheckCircleCircleInfo(aX, aY, colA.radius, bX, bY, colB.radius, out contactX, out contactY),

            (E_ColliderShape.Circle, E_ColliderShape.Rect) =>
                CheckCircleRectInfo(aX, aY, colA.radius, bX, bY, colB.width, colB.height, cosB, sinB, out contactX, out contactY),

            (E_ColliderShape.Rect, E_ColliderShape.Circle) =>
                CheckCircleRectInfo(bX, bY, colB.radius, aX, aY, colA.width, colA.height, cosA, sinA, out contactX, out contactY),

            (E_ColliderShape.Rect, E_ColliderShape.Rect) =>
                CheckRectRectInfo(aX, aY, colA.width, colA.height, cosA, sinA, bX, bY, colB.width, colB.height, cosB, sinB, out contactX, out contactY),

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

        if (distSq > rSum * rSum)
        {
            contactX = contactY = 0f;
            return false;
        }

        if (distSq == 0f)
        {
            contactX = aX;
            contactY = aY;
            return true;
        }

        float dist = MathF.Sqrt(distSq);
        float ratio = aR / dist;
        contactX = aX + dx * ratio;
        contactY = aY + dy * ratio;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckCircleRectInfo(float cX, float cY, float radius, 
                                    float rX, float rY, float w, float h, 
                                    float rCos, float rSin, 
                                    out float contactX, out float contactY)
    {
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;

        // 1. 将圆心变换到矩形的局部空间
        float dx = cX - rX;
        float dy = cY - rY;

        // 逆旋转
        float localCx = dx * rCos + dy * rSin;
        float localCy = -dx * rSin + dy * rCos;

        // 2. 局部空间 AABB vs Circle
        float closestLocalX = MathF.Max(-halfW, MathF.Min(localCx, halfW));
        float closestLocalY = MathF.Max(-halfH, MathF.Min(localCy, halfH));

        float distVecX = localCx - closestLocalX;
        float distVecY = localCy - closestLocalY;
        float distSq = distVecX * distVecX + distVecY * distVecY;

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

        // 3. 变换回世界坐标
        contactX = closestLocalX * rCos - closestLocalY * rSin + rX;
        contactY = closestLocalX * rSin + closestLocalY * rCos + rY;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CheckRectRectInfo(float aX, float aY, float aW, float aH, float aCos, float aSin,
                                  float bX, float bY, float bW, float bH, float bCos, float bSin,
                                  out float contactX, out float contactY)
    {
        // 1. 准备轴向
        float axisAX = aCos; float axisAY = aSin;
        float axisAY_X = -aSin; float axisAY_Y = aCos;

        float axisBX = bCos; float axisBY = bSin;
        float axisBY_X = -bSin; float axisBY_Y = bCos;

        // 2. 计算两中心点的向量
        float dx = bX - aX;
        float dy = bY - aY;

        float Dot(float x1, float y1, float x2, float y2) => x1 * x2 + y1 * y2;
        float Abs(float v) => v < 0 ? -v : v;

        bool Project(float axisX, float axisY, out float overlap)
        {
            float dist = Abs(Dot(dx, dy, axisX, axisY));
            float rA = 0.5f * (aW * Abs(Dot(axisX, axisY, axisAX, axisAY)) + aH * Abs(Dot(axisX, axisY, axisAY_X, axisAY_Y)));
            float rB = 0.5f * (bW * Abs(Dot(axisX, axisY, axisBX, axisBY)) + bH * Abs(Dot(axisX, axisY, axisBY_X, axisBY_Y)));

            overlap = (rA + rB) - dist;
            return overlap > 0;
        }

        // 3. 检测4个轴
        if (!Project(axisAX, axisAY, out _)) { contactX = contactY = 0; return false; }
        if (!Project(axisAY_X, axisAY_Y, out _)) { contactX = contactY = 0; return false; }
        if (!Project(axisBX, axisBY, out _)) { contactX = contactY = 0; return false; }
        if (!Project(axisBY_X, axisBY_Y, out _)) { contactX = contactY = 0; return false; }

        // 4. 接触点近似
        contactX = (aX + bX) * 0.5f;
        contactY = (aY + bY) * 0.5f;
        
        return true;
    }

    #endregion
}