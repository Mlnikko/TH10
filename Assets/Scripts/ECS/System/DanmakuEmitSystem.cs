using System;
using UnityEngine;

public class DanmakuEmitSystem : BaseSystem
{
    public override void OnLogicTick(uint currentFrame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CDanmakuEmitter>();

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();
        var emitters = EntityManager.GetComponentSpan<CDanmakuEmitter>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];

            var position = positions[idx];
            var rotation = rotations[idx];

            ref var emitter = ref emitters[idx];

            if (!emitter.isEmitting)
            {
                continue;
            }

            float emitRotRad = rotation.angleRad + emitter.emitterRotOffsetRad;
            ProcessEmission(ref emitter, position.x, position.y, emitRotRad, currentFrame);
        }
    }

    void ProcessEmission(ref CDanmakuEmitter emitter, float emitPosX, float emitPosY, float emitRotRad, uint currentFrame)
    {
        uint framesSinceLastFire = currentFrame - emitter.lastFireFrame;
        if (emitter.launchCooldownFrames > 0 && framesSinceLastFire < (uint)emitter.launchCooldownFrames)
            return;

        // 2. 获取当前要发射的弹幕配置索引 (处理选择模式)
        int danmakuCfgIndex = GetSelectedBulletIndex(ref emitter);
        if (danmakuCfgIndex == -1) return;

        // 3. 根据模式发射 (数据全在本地，无查表)
        switch (emitter.emitMode)
        {
            case EmitMode.Line:
                EmitLineOptimized(ref emitter, emitPosX, emitPosY, emitRotRad, danmakuCfgIndex);
                break;
            case EmitMode.Arc:
                EmitArcOptimized(ref emitter, emitPosX, emitPosY, emitRotRad, danmakuCfgIndex);
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

    void EmitLineOptimized(ref CDanmakuEmitter e, float emitPosX, float emitPosY, float emitRotRad, int cfgIndex)
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
        float cosR = Mathf.Cos(emitRotRad);
        float sinR = Mathf.Sin(emitRotRad);
        float spawnRotRad = e.danmakuRotOffsetRad;

        for (int i = 0; i < e.lineCount; i++)
        {
            float factor = (i - halfSpan) * spacing;

            // 2. 计算局部偏移向量 (未旋转)
            // offset = basePerp * factor
            float localOffX = basePerpX * factor;
            float localOffY = basePerpY * factor;

            // 3. 【核心修改】将“基准方向”和“偏移向量”都旋转 emitRotRad

            // A. 旋转后的发射方向 (速度方向)
            // dir = rotate(baseDir, emitRotRad)
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

            SpawnDanmaku(spawnX, spawnY, spawnRotRad, velX, velY, cfgIndex);
        }
    }

    void EmitArcOptimized(ref CDanmakuEmitter e, float emitPosX, float emitPosY, float emitRotRad, int cfgIndex)
    {
        // 提取局部变量
        float startRad = e.arcStartAngleRad; // 相对于发射器前方的起始角 (例如 -45 度)
        float stepRad = e.arcAngleStepRad * e.arcDirectionSign;
        float radius = e.arcRadius;
        float speed = e.launchSpeed;
        float offX = e.emitterPosOffsetX;
        float offY = e.emitterPosOffsetY;
        int count = e.arcBulletCount;

        float cosR = Mathf.Cos(emitRotRad);
        float sinR = Mathf.Sin(emitRotRad);

        // 旋转发射器中心偏移 (Gun Offset)
        float rotatedOffX = offX * cosR - offY * sinR;
        float rotatedOffY = offX * sinR + offY * cosR;
        float danmakuRotOffRad = e.danmakuRotOffsetRad;

        for (int i = 0; i < count; i++)
        {
            // 【关键修改】基础角度 + 发射器自身旋转
            float angle = emitRotRad + startRad + (stepRad * i);

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // 计算相对于圆心的偏移（世界空间；angle 已含发射器朝向 emitRotRad）
            float offsetX = cos * radius;
            float offsetY = sin * radius;

            // 最终位置 = 发射器世界位置 + 旋转后的枪口偏移 + 扇形分布偏移
            float spawnX = emitPosX + rotatedOffX + offsetX;
            float spawnY = emitPosY + rotatedOffY + offsetY;

            float spawnRotRad = angle + danmakuRotOffRad;

            // 速度方向就是当前角度方向
            float velX = cos * speed;
            float velY = sin * speed;

            // 弹幕旋转：通常等于其飞行角度
            SpawnDanmaku(spawnX, spawnY, spawnRotRad, velX, velY, cfgIndex);
        }
    }

    void SpawnDanmaku(float posX, float posY, float rotationRad, float velX, float velY, int cfgIndex)
    {
        Entity e_danmaku = EntityFactory.CreateDanmaku(posX, posY, rotationRad, velX, velY, cfgIndex);
        EntityManager.AddComponent(e_danmaku, new CPoolGetTag());
    }
}

