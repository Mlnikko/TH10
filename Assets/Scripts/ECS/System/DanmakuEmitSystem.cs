using System;
using UnityEngine;

public class DanmakuEmitSystem : BaseSystem
{
    public override void OnLogicTick(uint currentFrame)
    {
        Span<int> emitterIndices = TempBuffers.DanmakuEmitterIndices;
        int emitterCount = EntityManager.GetEntities<CDanmakuEmitter>(emitterIndices);

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();
        var emitters = EntityManager.GetComponentSpan<CDanmakuEmitter>();

        for (int i = 0; i < emitterCount; i++)
        {
            int entity = emitterIndices[i];

            var position = positions[entity];
            var rotation = rotations[entity];

            ref var emitter = ref emitters[entity];

            if (!emitter.isEmitting)
            {
                continue;
            }

            ProcessEmission(ref emitter, position.x, position.y, rotation.rotZ, currentFrame);
        }
    }

    void ProcessEmission(ref CDanmakuEmitter emitter, float emitPosX, float emitPosY, float emitRotZ, uint currentFrame)
    {
        uint framesSinceLastFire = currentFrame - emitter.lastFireFrame;
        // 1. 冷却检查 (纯整数)
        if (framesSinceLastFire * LogicFrameTimer.FrameInterval < emitter.launchInterval)
        {
            return;
        }

        // 2. 获取当前要发射的弹幕配置索引 (处理选择模式)
        int danmakuCfgIndex = GetSelectedBulletIndex(ref emitter);
        if (danmakuCfgIndex == -1) return;

        // 3. 根据模式发射 (数据全在本地，无查表)
        switch (emitter.emitMode)
        {
            case EmitMode.Line:
                EmitLineOptimized(ref emitter, emitPosX, emitPosY, emitRotZ, danmakuCfgIndex);
                break;
            case EmitMode.Arc:
                EmitArcOptimized(ref emitter, emitPosX, emitPosY, emitRotZ, danmakuCfgIndex);
                break;
            case EmitMode.None:
                Logger.Warn("发射器发射模式为None! 请检查配置");
                break;
        }

        emitter.lastFireFrame = currentFrame;
    }

    int GetSelectedBulletIndex(ref CDanmakuEmitter e)
    {
        if (e.danmakuCfgIndices.Length == 0) return -1;

        switch (e.selectMode)
        {
            case DanmakuSelectMode.First:
                return e.danmakuCfgIndices[0];

            case DanmakuSelectMode.Sequential:
                int idx = e.sequentialIndex % e.danmakuCfgIndices.Length;
                e.sequentialIndex++; // 更新组件内的状态
                return e.danmakuCfgIndices[idx];

            case DanmakuSelectMode.Random:
                // 必须使用确定性随机
                // int rand = DeterministicRandom.Next(ref e.randomSeed); 
                return e.danmakuCfgIndices[0]; // 占位
        }
        return -1;
    }

    void EmitLineOptimized(ref CDanmakuEmitter e, float emitPosX, float emitPosY, float emitRotZ, int cfgIndex)
    {
        // 1. 提取局部变量
        float baseDirX = e.lineDirUnitX; // 配置中的基准方向 (通常是 1, 0)
        float baseDirY = e.lineDirUnitY;
        float basePerpX = e.lineDirPerpX; // 配置中的垂直方向 (通常是 0, 1)
        float basePerpY = e.lineDirPerpY;

        float spacing = e.lineSpacingHalf * 2.0f;
        float halfSpan = (e.lineCount - 1) * 0.5f;
        float speed = e.launchSpeed;
        float offX = e.emitterPosOffsetX;
        float offY = e.emitterPosOffsetY;

        // 【关键】预先计算发射器旋转的 Sin/Cos，避免循环内重复计算
        float emitRad = emitRotZ * Mathf.Deg2Rad;
        float cosR = Mathf.Cos(emitRad);
        float sinR = Mathf.Sin(emitRad);

        for (int i = 0; i < e.lineCount; i++)
        {
            float factor = (i - halfSpan) * spacing;

            // 2. 计算局部偏移向量 (未旋转)
            // offset = basePerp * factor
            float localOffX = basePerpX * factor;
            float localOffY = basePerpY * factor;

            // 3. 【核心修改】将“基准方向”和“偏移向量”都旋转 emitRad

            // A. 旋转后的发射方向 (速度方向)
            // dir = rotate(baseDir, emitRad)
            float finalDirX = baseDirX * cosR - baseDirY * sinR;
            float finalDirY = baseDirX * sinR + baseDirY * cosR;

            // B. 旋转后的实际生成位置偏移
            // 注意：位置偏移 = 发射器中心偏移 (offX/Y) + 队列偏移 (localOffX/Y)
            // 这里假设 offX/Y 是沿着发射器前方/上方的偏移，通常也需要旋转，或者它已经是世界坐标？
            // 假设 offX/Y 也是本地坐标 (例如枪口偏移)，则必须旋转。

            // 总局部偏移 = (offX, offY) + (localOffX, localOffY)
            float totalLocalOffX = offX + localOffX;
            float totalLocalOffY = offY + localOffY;

            // 旋转总偏移
            float rotatedOffX = totalLocalOffX * cosR - totalLocalOffY * sinR;
            float rotatedOffY = totalLocalOffX * sinR + totalLocalOffY * cosR;

            // 4. 计算世界坐标
            float spawnX = emitPosX + rotatedOffX;
            float spawnY = emitPosY + rotatedOffY;

            // 5. 计算世界速度
            float velX = finalDirX * speed;
            float velY = finalDirY * speed;

            float spawnRotOffsetZ = e.danmakuRotOffsetZ; // 直接让弹幕面向飞行方向

            SpawnDanmaku(spawnX, spawnY, spawnRotOffsetZ, velX, velY, cfgIndex);
        }
    }

    void EmitArcOptimized(ref CDanmakuEmitter e, float emitPosX, float emitPosY, float emitRotZ, int cfgIndex)
    {
        // 提取局部变量
        float startRad = e.arcStartAngleRad; // 相对于发射器前方的起始角 (例如 -45 度)
        float stepRad = e.arcAngleStepRad * e.arcDirectionSign;
        float radius = e.arcRadius;
        float speed = e.launchSpeed;
        float offX = e.emitterPosOffsetX;
        float offY = e.emitterPosOffsetY;
        int count = e.arcBulletCount;

        float emitRad = emitRotZ * Mathf.Deg2Rad;
        // 预计算发射器偏移的旋转 (如果 offX/Y 不为 0)
        float cosR = Mathf.Cos(emitRad);
        float sinR = Mathf.Sin(emitRad);

        // 旋转发射器中心偏移 (Gun Offset)
        float rotatedOffX = offX * cosR - offY * sinR;
        float rotatedOffY = offX * sinR + offY * cosR;
            
        for (int i = 0; i < count; i++)
        {
            // 【关键修改】基础角度 + 发射器自身旋转
            float angle = emitRad + startRad + (stepRad * i);

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // 计算相对于圆心的偏移 (这是世界空间的偏移，因为 angle 已经包含了 emitRad)
            float offsetX = cos * radius;
            float offsetY = sin * radius;

            // 最终位置 = 发射器世界位置 + 旋转后的枪口偏移 + 扇形分布偏移
            float spawnX = emitPosX + rotatedOffX + offsetX;
            float spawnY = emitPosY + rotatedOffY + offsetY;

            // 弹幕本身的旋转：发射器角度 + 弹幕自身的角度偏移
            float spawnRotOffsetZ = angle + e.danmakuRotOffsetZ; // 直接让弹幕面向飞行方向

            // 速度方向就是当前角度方向
            float velX = cos * speed;
            float velY = sin * speed;

            // 弹幕旋转：通常等于其飞行角度
            SpawnDanmaku(spawnX, spawnY, spawnRotOffsetZ, velX, velY, cfgIndex);
        }
    }

    void SpawnDanmaku(float posX, float posY, float rotZ ,float velX, float velY, int cfgIndex)
    {
        Entity e_danmaku = EntityFactory.CreateDanmaku(posX, posY, rotZ, velX, velY, cfgIndex);
        EntityManager.AddComponent(e_danmaku, new CPoolGetTag());
    }
}

