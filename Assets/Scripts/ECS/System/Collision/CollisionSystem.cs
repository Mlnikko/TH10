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
        TempBitSets.CollisionSweptBroadphase.ClearAll();

        if (_grid == null) return;
        Span<int> activeColliders = TempBuffers.CollisionActive;
        Span<int> queryResults = TempBuffers.CollisionQuery;

        Span<int> indices = EntityManager.GetActiveIndices<CCollider>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();
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

            float cos = MathF.Cos(rot.angleRad);
            float sin = MathF.Sin(rot.angleRad);
            GetWorldColliderCenter(pos.x, pos.y, cos, sin, col, out float cx, out float cy);

            // 玩家弹幕本帧有位移时：粗测覆盖扫掠区域（圆=线段包络；矩形=起止 OBB 的世界 AABB 并集）。
            if (TryGetSweptPlayerDanmakuBroadphaseAABB(col, cx, cy, velocities[e].vx, velocities[e].vy, cos, sin,
                    out float sminX, out float sminY, out float smaxX, out float smaxY))
            {
                TempBitSets.CollisionSweptBroadphase.Set(e, true);
                TempBuffers.CollisionSweptAabbMinX[e] = sminX;
                TempBuffers.CollisionSweptAabbMinY[e] = sminY;
                TempBuffers.CollisionSweptAabbMaxX[e] = smaxX;
                TempBuffers.CollisionSweptAabbMaxY[e] = smaxY;
                _grid.InsertAABB(e, sminX, sminY, smaxX, smaxY);
            }
            else
                _grid.Insert(e, cx, cy, col);
        }

        // Step 3: 检测碰撞
        for (int iIdx = 0; iIdx < colliderCount; iIdx++)
        {
            int i = activeColliders[iIdx];
            ref readonly var colA = ref colliders[i];

            ref readonly var posA = ref positions[i];
            ref readonly var rotA = ref rotations[i];

            float angleARad = rotA.angleRad;
            float cosA = MathF.Cos(angleARad);
            float sinA = MathF.Sin(angleARad);

            float ax = posA.x + (colA.offsetX * cosA - colA.offsetY * sinA);
            float ay = posA.y + (colA.offsetX * sinA + colA.offsetY * cosA);

            int queryCount = TempBitSets.CollisionSweptBroadphase.Get(i)
                ? _grid.QueryAABB(
                    TempBuffers.CollisionSweptAabbMinX[i],
                    TempBuffers.CollisionSweptAabbMinY[i],
                    TempBuffers.CollisionSweptAabbMaxX[i],
                    TempBuffers.CollisionSweptAabbMaxY[i],
                    queryResults,
                    TempBitSets.Collision)
                : _grid.Query(ax, ay, colA, queryResults, TempBitSets.Collision);

            for (int k = 0; k < queryCount; k++)
            {
                int j = queryResults[k];
                if (j <= i) continue;

                ref readonly var colB = ref colliders[j];
                ref readonly var posB = ref positions[j];
                ref readonly var rotB = ref rotations[j];

                float angleBRad = rotB.angleRad;
                float cosB = MathF.Cos(angleBRad);
                float sinB = MathF.Sin(angleBRad);

                float bx = posB.x + (colB.offsetX * cosB - colB.offsetY * sinB);
                float by = posB.y + (colB.offsetX * sinB + colB.offsetY * cosB);

                // 层级过滤
                if ((colA.mask & colB.layer) == 0) continue;
                if ((colB.mask & colA.layer) == 0) continue;

                ref readonly var velA = ref velocities[i];
                ref readonly var velB = ref velocities[j];

                if (!TryPairCollision(colA, ax, ay, cosA, sinA, velA.vx, velA.vy,
                        colB, bx, by, cosB, sinB, velB.vx, velB.vy,
                        out float contactX, out float contactY))
                    continue;

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
#if UNITY_EDITOR
                // Logger.Info($"[Collision] Detected collision between Entity {evt.EntityA} and Entity {evt.EntityB} at ({evt.ContactX}, {evt.ContactY}) on frame {currentframe}", LogTag.Collision);
#endif
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

    /// <summary>
    /// 碰撞点世界坐标（含旋转偏移后的判定中心）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void GetWorldColliderCenter(float posX, float posY, float angleRad, in CCollider col, out float cx, out float cy)
    {
        float cos = MathF.Cos(angleRad);
        float sin = MathF.Sin(angleRad);
        GetWorldColliderCenter(posX, posY, cos, sin, col, out cx, out cy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void GetWorldColliderCenter(float posX, float posY, float cos, float sin, in CCollider col, out float cx, out float cy)
    {
        cx = posX + (col.offsetX * cos - col.offsetY * sin);
        cy = posY + (col.offsetX * sin + col.offsetY * cos);
    }

    /// <summary>
    /// 本帧即将位移的玩家弹幕粗测包络（与 <see cref="DanmakuSystem"/> 位移一致）：圆=线段±半径；矩形=起止旋转矩形的世界 AABB 并集。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryGetSweptPlayerDanmakuBroadphaseAABB(in CCollider col, float cx, float cy,
        float vx, float vy, float bulletCos, float bulletSin,
        out float minX, out float minY, out float maxX, out float maxY)
    {
        minX = minY = maxX = maxY = 0f;
        if (!col.isActive || col.layer != E_ColliderLayer.PlayerDanmaku)
            return false;

        float vSq = vx * vx + vy * vy;
        if (vSq < 1e-12f)
            return false;

        switch (col.shape)
        {
            case E_ColliderShape.Circle:
            {
                float r = col.radius;
                float cx1 = cx + vx;
                float cy1 = cy + vy;
                minX = MathF.Min(cx, cx1) - r;
                maxX = MathF.Max(cx, cx1) + r;
                minY = MathF.Min(cy, cy1) - r;
                maxY = MathF.Max(cy, cy1) + r;
                return true;
            }
            case E_ColliderShape.Rect:
            {
                GetOrientedRectWorldAABB(cx, cy, col.width, col.height, bulletCos, bulletSin, out minX, out minY, out maxX, out maxY);
                GetOrientedRectWorldAABB(cx + vx, cy + vy, col.width, col.height, bulletCos, bulletSin,
                    out float ax2, out float ay2, out float bx2, out float by2);
                minX = MathF.Min(minX, ax2);
                minY = MathF.Min(minY, ay2);
                maxX = MathF.Max(maxX, bx2);
                maxY = MathF.Max(maxY, by2);
                return true;
            }
            default:
                return false;
        }
    }

    /// <summary>旋转矩形判定盒四角的世界轴对齐包围盒。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void GetOrientedRectWorldAABB(float rectCx, float rectCy, float w, float h, float cos, float sin,
        out float minX, out float minY, out float maxX, out float maxY)
    {
        float hw = w * 0.5f;
        float hh = h * 0.5f;

        float Wx(float lx, float ly) => rectCx + lx * cos - ly * sin;
        float Wy(float lx, float ly) => rectCy + lx * sin + ly * cos;

        // 不得在内层本地函数里写入外层方法的 out 参数；先用局部变量聚合。
        float bx = Wx(-hw, -hh);
        float by = Wy(-hw, -hh);
        float bminX = bx;
        float bmaxX = bx;
        float bminY = by;
        float bmaxY = by;

        void Corner(float lx, float ly)
        {
            float wx = Wx(lx, ly);
            float wy = Wy(lx, ly);
            bminX = MathF.Min(bminX, wx);
            bmaxX = MathF.Max(bmaxX, wx);
            bminY = MathF.Min(bminY, wy);
            bmaxY = MathF.Max(bmaxY, wy);
        }

        Corner(hw, -hh);
        Corner(hw, hh);
        Corner(-hw, hh);

        minX = bminX;
        maxX = bmaxX;
        minY = bminY;
        maxY = bmaxY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryPairCollision(
        in CCollider colA, float aX, float aY, float cosA, float sinA, float avx, float avy,
        in CCollider colB, float bX, float bY, float cosB, float sinB, float bvx, float bvy,
        out float contactX, out float contactY)
    {
        if (TrySweptPlayerDanmakuVsEnemy(colA, aX, aY, avx, avy, cosA, sinA, colB, bX, bY, cosB, sinB, out contactX, out contactY))
            return true;
        if (TrySweptPlayerDanmakuVsEnemy(colB, bX, bY, bvx, bvy, cosB, sinB, colA, aX, aY, cosA, sinA, out contactX, out contactY))
            return true;
        return TryGetCollisionInfo(colA, aX, aY, cosA, sinA, colB, bX, bY, cosB, sinB, out contactX, out contactY);
    }

    /// <summary>
    /// 玩家弹幕相对本帧位移的扫掠检测（圆/矩形）；敌人位置取当前逻辑帧已更新后的中心。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TrySweptPlayerDanmakuVsEnemy(
        in CCollider bulletCol, float bulletCx, float bulletCy, float bvx, float bvy,
        float bulletCos, float bulletSin,
        in CCollider enemyCol, float enemyCx, float enemyCy, float enemyCos, float enemySin,
        out float contactX, out float contactY)
    {
        contactX = contactY = 0f;

        if (!bulletCol.isActive || bulletCol.layer != E_ColliderLayer.PlayerDanmaku)
            return false;
        if (bvx * bvx + bvy * bvy < 1e-12f)
            return false;
        if (enemyCol.layer != E_ColliderLayer.Enemy)
            return false;

        float bx1 = bulletCx + bvx;
        float by1 = bulletCy + bvy;

        if (bulletCol.shape == E_ColliderShape.Circle)
        {
            float ar = bulletCol.radius;
            return enemyCol.shape switch
            {
                E_ColliderShape.Circle =>
                    SweptCircleVsCircle(bulletCx, bulletCy, bx1, by1, ar, enemyCx, enemyCy, enemyCol.radius, out contactX, out contactY),
                E_ColliderShape.Rect =>
                    SweptCircleVsOrientedRect(bulletCx, bulletCy, bx1, by1, ar, enemyCx, enemyCy, enemyCol.width, enemyCol.height, enemyCos, enemySin, out contactX, out contactY),
                _ => false
            };
        }

        if (bulletCol.shape == E_ColliderShape.Rect)
        {
            float bw = bulletCol.width;
            float bh = bulletCol.height;
            return enemyCol.shape switch
            {
                E_ColliderShape.Circle =>
                    SweptOrientedRectVsCircle(bulletCx, bulletCy, bvx, bvy, bw, bh, bulletCos, bulletSin,
                        enemyCx, enemyCy, enemyCol.radius, out contactX, out contactY),
                E_ColliderShape.Rect =>
                    SweptOrientedRectVsOrientedRect(bulletCx, bulletCy, bvx, bvy, bw, bh, bulletCos, bulletSin,
                        enemyCx, enemyCy, enemyCol.width, enemyCol.height, enemyCos, enemySin, out contactX, out contactY),
                _ => false
            };
        }

        return false;
    }

    /// <summary>矩形弹幕沿位移扫掠 vs 圆敌：自适应离散步 + 二分取首次重叠（确定性）。</summary>
    static bool SweptOrientedRectVsCircle(
        float bulletCx0, float bulletCy0, float bvx, float bvy,
        float bulletW, float bulletH, float bulletCos, float bulletSin,
        float enemyCx, float enemyCy, float enemyR,
        out float contactX, out float contactY)
    {
        contactX = contactY = 0f;

        int scanSteps = ComputeBulletMotionScanSteps(bvx, bvy, bulletW, bulletH, enemyR);

        bool OverlapAt(float t)
        {
            float tcx = bulletCx0 + t * bvx;
            float tcy = bulletCy0 + t * bvy;
            return CheckCircleRectInfo(enemyCx, enemyCy, enemyR, tcx, tcy, bulletW, bulletH, bulletCos, bulletSin, out _, out _);
        }

        if (OverlapAt(0f))
            return CheckCircleRectInfo(enemyCx, enemyCy, enemyR, bulletCx0, bulletCy0, bulletW, bulletH, bulletCos, bulletSin, out contactX, out contactY);

        float prevT = 0f;
        for (int s = 1; s <= scanSteps; s++)
        {
            float t = s / (float)scanSteps;
            if (OverlapAt(t))
            {
                float lo = prevT;
                float hi = t;
                for (int i = 0; i < 12; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if (OverlapAt(mid))
                        hi = mid;
                    else
                        lo = mid;
                }

                float tHit = hi;
                float hx = bulletCx0 + tHit * bvx;
                float hy = bulletCy0 + tHit * bvy;
                return CheckCircleRectInfo(enemyCx, enemyCy, enemyR, hx, hy, bulletW, bulletH, bulletCos, bulletSin, out contactX, out contactY);
            }

            prevT = t;
        }

        return false;
    }

    /// <summary>矩形弹幕沿位移扫掠 vs 矩形敌。</summary>
    static bool SweptOrientedRectVsOrientedRect(
        float bulletCx0, float bulletCy0, float bvx, float bvy,
        float bulletW, float bulletH, float bulletCos, float bulletSin,
        float enemyCx, float enemyCy, float enemyW, float enemyH, float enemyCos, float enemySin,
        out float contactX, out float contactY)
    {
        contactX = contactY = 0f;

        float enemyMinDim = MathF.Min(enemyW, enemyH);
        int scanSteps = ComputeBulletMotionScanSteps(bvx, bvy, bulletW, bulletH, enemyMinDim * 0.5f);

        bool OverlapAt(float t)
        {
            float tcx = bulletCx0 + t * bvx;
            float tcy = bulletCy0 + t * bvy;
            return CheckRectRectInfo(tcx, tcy, bulletW, bulletH, bulletCos, bulletSin,
                enemyCx, enemyCy, enemyW, enemyH, enemyCos, enemySin, out _, out _);
        }

        if (OverlapAt(0f))
            return CheckRectRectInfo(bulletCx0, bulletCy0, bulletW, bulletH, bulletCos, bulletSin,
                enemyCx, enemyCy, enemyW, enemyH, enemyCos, enemySin, out contactX, out contactY);

        float prevT = 0f;
        for (int s = 1; s <= scanSteps; s++)
        {
            float t = s / (float)scanSteps;
            if (OverlapAt(t))
            {
                float lo = prevT;
                float hi = t;
                for (int i = 0; i < 12; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if (OverlapAt(mid))
                        hi = mid;
                    else
                        lo = mid;
                }

                float tHit = hi;
                float hx = bulletCx0 + tHit * bvx;
                float hy = bulletCy0 + tHit * bvy;
                return CheckRectRectInfo(hx, hy, bulletW, bulletH, bulletCos, bulletSin,
                    enemyCx, enemyCy, enemyW, enemyH, enemyCos, enemySin, out contactX, out contactY);
            }

            prevT = t;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int ComputeBulletMotionScanSteps(float bvx, float bvy, float bulletW, float bulletH, float enemyCharacteristicSize)
    {
        float segLen = MathF.Sqrt(bvx * bvx + bvy * bvy);
        float minBullet = MathF.Min(bulletW, bulletH);
        float stepSize = MathF.Max(0.04f, MathF.Min(minBullet * 0.25f, MathF.Max(enemyCharacteristicSize * 0.5f, 0.02f)));
        return Math.Clamp(8 + (int)MathF.Ceiling(segLen / stepSize), 8, 64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool SweptCircleVsCircle(float ax0, float ay0, float ax1, float ay1, float ar,
        float bX, float bY, float br,
        out float contactX, out float contactY)
    {
        float rSum = ar + br;
        float rsq = rSum * rSum;

        float sx = bX - ax0, sy = bY - ay0;
        if (sx * sx + sy * sy <= rsq)
            return CheckCircleCircleInfo(ax0, ay0, ar, bX, bY, br, out contactX, out contactY);

        float ex = bX - ax1, ey = bY - ay1;
        if (ex * ex + ey * ey <= rsq)
            return CheckCircleCircleInfo(ax1, ay1, ar, bX, bY, br, out contactX, out contactY);

        float abx = ax1 - ax0, aby = ay1 - ay0;
        float acx = bX - ax0, acy = bY - ay0;
        float abLenSq = abx * abx + aby * aby;
        float t = abLenSq > 1e-12f ? MathF.Max(0f, MathF.Min(1f, (acx * abx + acy * aby) / abLenSq)) : 0f;
        float px = ax0 + t * abx, py = ay0 + t * aby;
        float dx = bX - px, dy = bY - py;
        if (dx * dx + dy * dy > rsq)
        {
            contactX = contactY = 0f;
            return false;
        }

        return CheckCircleCircleInfo(px, py, ar, bX, bY, br, out contactX, out contactY);
    }

    /// <summary>
    /// 将圆形弹幕轨迹在矩形局部空间用膨胀 AABB 近似（角部略保守，略增命中；性能友好）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool SweptCircleVsOrientedRect(
        float ax0, float ay0, float ax1, float ay1, float bulletRadius,
        float rectCx, float rectCy, float rectW, float rectH,
        float rCos, float rSin,
        out float contactX, out float contactY)
    {
        float halfW = rectW * 0.5f;
        float halfH = rectH * 0.5f;
        float inflate = bulletRadius;

        WorldToLocal(ax0, ay0, rectCx, rectCy, rCos, rSin, out float l0x, out float l0y);
        WorldToLocal(ax1, ay1, rectCx, rectCy, rCos, rSin, out float l1x, out float l1y);

        if (!SegmentVsAABBClipped(l0x, l0y, l1x, l1y,
                -halfW - inflate, -halfH - inflate, halfW + inflate, halfH + inflate,
                out float tHit))
        {
            contactX = contactY = 0f;
            return false;
        }

        float lx = l0x + tHit * (l1x - l0x);
        float ly = l0y + tHit * (l1y - l0y);
        contactX = rectCx + lx * rCos - ly * rSin;
        contactY = rectCy + lx * rSin + ly * rCos;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WorldToLocal(float wx, float wy, float rectCx, float rectCy, float rCos, float rSin, out float lx, out float ly)
    {
        float dx = wx - rectCx, dy = wy - rectCy;
        lx = dx * rCos + dy * rSin;
        ly = -dx * rSin + dy * rCos;
    }

    /// <summary>线段 P(t)=P0+t(P1-P0), t∈[0,1] 与轴对齐矩形相交。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool SegmentVsAABBClipped(float x0, float y0, float x1, float y1,
        float minX, float minY, float maxX, float maxY,
        out float tEnter)
    {
        float dx = x1 - x0, dy = y1 - y0;
        float t0 = 0f, t1 = 1f;
        const float eps = 1e-12f;

        void ClipAxis(float p0, float dp, float minV, float maxV)
        {
            if (MathF.Abs(dp) < eps)
            {
                if (p0 < minV || p0 > maxV)
                    t0 = 2f;
                return;
            }

            float inv = 1f / dp;
            float tA = (minV - p0) * inv;
            float tB = (maxV - p0) * inv;
            if (tA > tB)
            {
                float tmp = tA;
                tA = tB;
                tB = tmp;
            }

            if (tA > t0)
                t0 = tA;
            if (tB < t1)
                t1 = tB;
        }

        ClipAxis(x0, dx, minX, maxX);
        if (t0 > t1)
        {
            tEnter = 0f;
            return false;
        }

        ClipAxis(y0, dy, minY, maxY);
        if (t0 > t1)
        {
            tEnter = 0f;
            return false;
        }

        tEnter = MathF.Max(0f, t0);
        return true;
    }

    #endregion
}