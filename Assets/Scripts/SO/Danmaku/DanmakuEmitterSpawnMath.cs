using System;
using UnityEngine;

/// <summary>
/// 弹幕发射器生成点与初速度计算（<see cref="DanmakuEmitSystem"/> 与编辑器预览共用）。
/// </summary>
public static class DanmakuEmitterSpawnMath
{
    public struct SpawnSample
    {
        public float posX;
        public float posY;
        public float rotRad;
        public float velX;
        public float velY;
    }

    public delegate void SpawnHandler(float posX, float posY, float rotRad, float velX, float velY);

    public static void EmitLine(
        in CDanmakuEmitter emitter,
        float emitPosX,
        float emitPosY,
        float emitRotRad,
        SpawnHandler spawn)
    {
        if (spawn == null)
            return;

        float baseDirX = emitter.lineDirUnitX;
        float baseDirY = emitter.lineDirUnitY;
        float basePerpX = emitter.lineDirPerpX;
        float basePerpY = emitter.lineDirPerpY;

        float spacing = emitter.lineSpacingHalf * 2f;
        float halfSpan = (emitter.lineCount - 1) * 0.5f;
        float speed = emitter.launchSpeed;
        float offX = emitter.emitterPosOffsetX;
        float offY = emitter.emitterPosOffsetY;

        float cosR = Mathf.Cos(emitRotRad);
        float sinR = Mathf.Sin(emitRotRad);
        float spawnRotRad = emitter.danmakuRotOffsetRad;

        for (int i = 0; i < emitter.lineCount; i++)
        {
            float factor = (i - halfSpan) * spacing;

            float localOffX = basePerpX * factor;
            float localOffY = basePerpY * factor;

            float finalDirX = baseDirX * cosR - baseDirY * sinR;
            float finalDirY = baseDirX * sinR + baseDirY * cosR;

            float totalLocalOffX = offX + localOffX;
            float totalLocalOffY = offY + localOffY;

            float rotatedOffX = totalLocalOffX * cosR - totalLocalOffY * sinR;
            float rotatedOffY = totalLocalOffX * sinR + totalLocalOffY * cosR;

            float spawnX = emitPosX + rotatedOffX;
            float spawnY = emitPosY + rotatedOffY;

            float velX = finalDirX * speed;
            float velY = finalDirY * speed;

            spawn(spawnX, spawnY, spawnRotRad, velX, velY);
        }
    }

    public static void EmitArc(
        in CDanmakuEmitter emitter,
        float emitPosX,
        float emitPosY,
        float emitRotRad,
        SpawnHandler spawn)
    {
        EmitArcWithStart(
            in emitter, emitPosX, emitPosY, emitRotRad,
            emitter.arcStartAngleRad, spawn);
    }

    /// <summary>波弹：扇形中心角随逻辑帧正弦摆动。</summary>
    public static void EmitWave(
        in CDanmakuEmitter emitter,
        float emitPosX,
        float emitPosY,
        float emitRotRad,
        uint logicFrame,
        SpawnHandler spawn)
    {
        if (spawn == null)
            return;

        float phase = logicFrame * emitter.waveOmegaRadPerFrame + emitter.wavePhaseOffsetRad;
        float swing = Mathf.Sin(phase) * emitter.waveSwingRad;
        float dynamicStart = emitter.waveCenterAngleRad + swing - emitter.waveArcHalfSpreadRad;

        EmitArcWithStart(in emitter, emitPosX, emitPosY, emitRotRad, dynamicStart, spawn);
    }

    /// <summary>粒弹：锥形内确定性随机散布（锁步友好）。</summary>
    public static void EmitGrain(
        in CDanmakuEmitter emitter,
        float emitPosX,
        float emitPosY,
        float emitRotRad,
        int salvoIndex,
        SpawnHandler spawn)
    {
        if (spawn == null)
            return;

        float offX = emitter.emitterPosOffsetX;
        float offY = emitter.emitterPosOffsetY;
        float cosR = Mathf.Cos(emitRotRad);
        float sinR = Mathf.Sin(emitRotRad);
        float rotatedOffX = offX * cosR - offY * sinR;
        float rotatedOffY = offX * sinR + offY * cosR;
        float danmakuRotOffRad = emitter.danmakuRotOffsetRad;
        float scatter = emitter.grainSpawnScatterRadius;
        uint seed = emitter.randomSeed;

        for (int i = 0; i < emitter.grainBulletCount; i++)
        {
            float angleT = Deterministic01(seed, salvoIndex, i, 0);
            float speedT = Deterministic01(seed, salvoIndex, i, 1);
            float scatterX = Deterministic01(seed, salvoIndex, i, 2) - 0.5f;
            float scatterY = Deterministic01(seed, salvoIndex, i, 3) - 0.5f;

            float angle = emitRotRad + emitter.grainBaseAngleRad
                          + Mathf.Lerp(-emitter.grainConeHalfRad, emitter.grainConeHalfRad, angleT);
            float speed = Mathf.Lerp(emitter.grainSpeedMin, emitter.grainSpeedMax, speedT);

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            float spawnX = emitPosX + rotatedOffX;
            float spawnY = emitPosY + rotatedOffY;
            if (scatter > 1e-6f)
            {
                spawnX += scatterX * 2f * scatter;
                spawnY += scatterY * 2f * scatter;
            }

            spawn(spawnX, spawnY, angle + danmakuRotOffRad, cos * speed, sin * speed);
        }
    }

    static void EmitArcWithStart(
        in CDanmakuEmitter emitter,
        float emitPosX,
        float emitPosY,
        float emitRotRad,
        float startRadLocal,
        SpawnHandler spawn)
    {
        float stepRad = emitter.arcAngleStepRad * emitter.arcDirectionSign;
        float radius = emitter.arcRadius;
        float speed = emitter.launchSpeed;
        float offX = emitter.emitterPosOffsetX;
        float offY = emitter.emitterPosOffsetY;
        int count = emitter.arcBulletCount;

        float cosR = Mathf.Cos(emitRotRad);
        float sinR = Mathf.Sin(emitRotRad);

        float rotatedOffX = offX * cosR - offY * sinR;
        float rotatedOffY = offX * sinR + offY * cosR;
        float danmakuRotOffRad = emitter.danmakuRotOffsetRad;

        for (int i = 0; i < count; i++)
        {
            float angle = emitRotRad + startRadLocal + stepRad * i;

            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            float offsetX = cos * radius;
            float offsetY = sin * radius;

            float spawnX = emitPosX + rotatedOffX + offsetX;
            float spawnY = emitPosY + rotatedOffY + offsetY;

            float spawnRotRad = angle + danmakuRotOffRad;
            float velX = cos * speed;
            float velY = sin * speed;

            spawn(spawnX, spawnY, spawnRotRad, velX, velY);
        }
    }

    public static void CollectSpawns(
        in CDanmakuEmitter emitter,
        float emitPosX,
        float emitPosY,
        float emitRotRad,
        System.Collections.Generic.List<SpawnSample> output,
        uint logicFrame = 0,
        int salvoIndex = 0)
    {
        if (output == null)
            return;

        void Add(float x, float y, float rot, float vx, float vy)
        {
            output.Add(new SpawnSample
            {
                posX = x,
                posY = y,
                rotRad = rot,
                velX = vx,
                velY = vy,
            });
        }

        switch (emitter.emitMode)
        {
            case EmitMode.Line:
                EmitLine(in emitter, emitPosX, emitPosY, emitRotRad, Add);
                break;
            case EmitMode.Arc:
                EmitArc(in emitter, emitPosX, emitPosY, emitRotRad, Add);
                break;
            case EmitMode.Wave:
                EmitWave(in emitter, emitPosX, emitPosY, emitRotRad, logicFrame, Add);
                break;
            case EmitMode.Grain:
                EmitGrain(in emitter, emitPosX, emitPosY, emitRotRad, salvoIndex, Add);
                break;
        }
    }

    /// <summary>确定性 [0,1) 伪随机，用于粒弹散布（与帧同步兼容）。</summary>
    public static float Deterministic01(uint seed, int salvoIndex, int bulletIndex, int salt)
    {
        uint x = seed
                 + (uint)salvoIndex * 3266489917u
                 + (uint)bulletIndex * 668265263u
                 + (uint)salt * 374761393u;
        x ^= x >> 16;
        x *= 2246822519u;
        x ^= x >> 13;
        x *= 3266489917u;
        x ^= x >> 16;
        return (x & 0xffffffu) / (float)0x1000000u;
    }
}
