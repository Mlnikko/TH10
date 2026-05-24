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
        if (spawn == null)
            return;

        float startRad = emitter.arcStartAngleRad;
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
            float angle = emitRotRad + startRad + stepRad * i;

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
        System.Collections.Generic.List<SpawnSample> output)
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
        }
    }
}
