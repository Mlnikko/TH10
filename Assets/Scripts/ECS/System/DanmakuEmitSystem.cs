using System;

using UnityEngine;



public class DanmakuEmitSystem : BaseSystem

{

    public override void OnLogicTick(uint currentFrame)

    {

        EnemyOwnedEmitterLogic.SyncOwnedEmitters(EntityManager);

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

                continue;



            float emitRotRad = DanmakuEmitterAimAtPlayerLogic.ResolveEmitRotRad(
                in emitter,
                position.x,
                position.y,
                rotation.angleRad,
                EntityManager);

            ProcessEmission(ref emitter, position.x, position.y, emitRotRad, currentFrame);

        }

    }



    void ProcessEmission(ref CDanmakuEmitter emitter, float emitPosX, float emitPosY, float emitRotRad, uint currentFrame)
    {
        if (emitter.launchCountMax > 0 && emitter.launchCountUsed >= emitter.launchCountMax)
        {
            emitter.isEmitting = false;
            return;
        }

        if (emitter.activationFrame == DanmakuEmitterSalvoInfo.ActivationFrameUnset)
            emitter.activationFrame = currentFrame;

        if (emitter.launchCountUsed == 0 && emitter.initialLaunchDelayFrames > 0)
        {
            uint elapsedSinceActivation = currentFrame - emitter.activationFrame;
            if (elapsedSinceActivation < (uint)emitter.initialLaunchDelayFrames)
                return;
        }

        uint framesSinceLastFire = currentFrame - emitter.lastFireFrame;
        if (emitter.launchCooldownFrames > 0 && framesSinceLastFire < (uint)emitter.launchCooldownFrames)
            return;

        int danmakuCfgIndex = GetSelectedBulletIndex(ref emitter);
        if (danmakuCfgIndex == -1)
            return;

        int salvoIndex = emitter.launchCountUsed;
        bool fired = false;
        switch (emitter.emitMode)
        {
            case EmitMode.Line:
                DanmakuEmitterSpawnMath.EmitLine(
                    in emitter, emitPosX, emitPosY, emitRotRad,
                    (x, y, rot, vx, vy) => SpawnDanmaku(x, y, rot, vx, vy, danmakuCfgIndex));
                fired = true;
                break;

            case EmitMode.Arc:
                DanmakuEmitterSpawnMath.EmitArc(
                    in emitter, emitPosX, emitPosY, emitRotRad, salvoIndex,
                    (x, y, rot, vx, vy) => SpawnDanmaku(x, y, rot, vx, vy, danmakuCfgIndex));
                fired = true;
                break;

            case EmitMode.Wave:
                DanmakuEmitterSpawnMath.EmitWave(
                    in emitter, emitPosX, emitPosY, emitRotRad, currentFrame, salvoIndex,
                    (x, y, rot, vx, vy) => SpawnDanmaku(x, y, rot, vx, vy, danmakuCfgIndex));
                fired = true;
                break;

            case EmitMode.Grain:
                DanmakuEmitterSpawnMath.EmitGrain(
                    in emitter, emitPosX, emitPosY, emitRotRad, salvoIndex,
                    (x, y, rot, vx, vy) => SpawnDanmaku(x, y, rot, vx, vy, danmakuCfgIndex));
                fired = true;
                break;

            case EmitMode.None:
                Logger.Warn("发射器发射模式为None! 请检查配置");
                break;
        }

        if (!fired)
            return;

        emitter.lastFireFrame = currentFrame;
        emitter.launchCountUsed++;
        if (emitter.launchCountMax > 0 && emitter.launchCountUsed >= emitter.launchCountMax)
            emitter.isEmitting = false;
    }



    int GetSelectedBulletIndex(ref CDanmakuEmitter e)

    {

        if (e.danmakuCfgIndices.Length == 0)

            return -1;



        switch (e.selectMode)

        {

            case DanmakuSelectMode.First:

                return e.danmakuCfgIndices[0];



            case DanmakuSelectMode.Sequential:

                int idx = e.sequentialIndex % e.danmakuCfgIndices.Length;

                e.sequentialIndex++;

                return e.danmakuCfgIndices[idx];



            case DanmakuSelectMode.Random:

                return e.danmakuCfgIndices[0];

        }



        return -1;

    }



    void SpawnDanmaku(float posX, float posY, float rotationRad, float velX, float velY, int cfgIndex)

    {

        Entity e_danmaku = EntityFactory.CreateDanmaku(posX, posY, rotationRad, velX, velY, cfgIndex);

        EntityManager.AddComponent(e_danmaku, new CPoolGetTag());

    }

}

