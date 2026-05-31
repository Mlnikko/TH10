using System;
using UnityEngine;

/// <summary>
/// 掉落物出场运动积分（逻辑帧确定性，供 <see cref="DropItemSystem"/> 与编辑器预览共用）。
/// </summary>
public static class DropItemMotionSimulator
{
    public static CDropItemMotion CreateMotionFromConfig(DropItemConfig cfg, uint logicFps)
        => CreateMotionFromConfig(cfg, logicFps, 0f, 0f, false);

    public static CDropItemMotion CreateMotionFromConfig(
        DropItemConfig cfg,
        uint logicFps,
        float burstDirX,
        float burstDirY,
        bool useBurstDirectionOverride)
    {
        cfg.BakeLogicTiming(logicFps);
        if (cfg.motionMode == E_DropMotionMode.DirectionalBurstThenFall)
        {
            ResolveBurstDirection(cfg, burstDirX, burstDirY, useBurstDirectionOverride, out float dirX, out float dirY);
            return new CDropItemMotion
            {
                motionMode = E_DropMotionMode.DirectionalBurstThenFall,
                motionPhase = 0,
                burstSpeedPerFrame = cfg.burstInitialPerFrame,
                burstDirX = dirX,
                burstDirY = dirY,
                burstDecelPerFrame = cfg.burstDecelPerFrame,
                fallVyPerFrame = cfg.fallVyAfterBurstPerFrame,
            };
        }

        return new CDropItemMotion
        {
            motionMode = E_DropMotionMode.VerticalToss,
            vyPerFrame = cfg.initialUpPerFrame,
            gravityPerFrame = cfg.gravityPerFrame,
            maxFallPerFrame = cfg.maxFallPerFrame,
            spinRadPerFrame = cfg.spinRadPerFrame,
        };
    }

    static void ResolveBurstDirection(
        DropItemConfig cfg,
        float burstDirX,
        float burstDirY,
        bool useOverride,
        out float dirX,
        out float dirY)
    {
        if (useOverride)
        {
            float lenSq = burstDirX * burstDirX + burstDirY * burstDirY;
            if (lenSq > 1e-8f)
            {
                float invLen = 1f / MathF.Sqrt(lenSq);
                dirX = burstDirX * invLen;
                dirY = burstDirY * invLen;
                return;
            }
        }

        dirX = cfg.burstDirX;
        dirY = cfg.burstDirY;
    }

    /// <summary>本帧位移（世界单位），并更新运动状态。</summary>
    public static void StepMotion(ref CDropItemMotion motion, out float dx, out float dy, out bool wasRising)
    {
        wasRising = false;
        dx = 0f;
        dy = 0f;

        if (motion.motionMode == E_DropMotionMode.DirectionalBurstThenFall)
        {
            StepDirectionalBurst(ref motion, out dx, out dy);
            return;
        }

        wasRising = motion.vyPerFrame > 0f;
        dy = StepVertical(ref motion);
    }

    static void StepDirectionalBurst(ref CDropItemMotion motion, out float dx, out float dy)
    {
        if (motion.motionPhase == 0)
        {
            float speed = motion.burstSpeedPerFrame;
            dx = motion.burstDirX * speed;
            dy = motion.burstDirY * speed;
            speed = Mathf.Max(0f, speed - motion.burstDecelPerFrame);
            motion.burstSpeedPerFrame = speed;
            if (speed <= 0f)
                motion.motionPhase = 1;
            return;
        }

        dx = 0f;
        dy = motion.fallVyPerFrame;
    }

    /// <summary>
    /// 上升阶段累计自转；越过最高点的当帧及之后保持角度为 0。
    /// </summary>
    public static void StepAscentRotation(bool wasRising, in CDropItemMotion motion, ref CRotation rotation)
    {
        if (motion.motionMode != E_DropMotionMode.VerticalToss)
            return;

        if (wasRising && motion.vyPerFrame > 0f)
            rotation.angleRad += motion.spinRadPerFrame;
        else
            rotation.angleRad = 0f;
    }

    public static void IntegrateVerticalMotion(ref CDropItemMotion motion)
    {
        motion.vyPerFrame -= motion.gravityPerFrame;

        float terminalDown = -motion.maxFallPerFrame;
        if (motion.vyPerFrame < terminalDown)
            motion.vyPerFrame = terminalDown;
    }

    /// <summary>本帧竖直位移（向上为正），并更新下一帧速度。</summary>
    public static float StepVertical(ref CDropItemMotion motion)
    {
        float dy = motion.vyPerFrame;
        IntegrateVerticalMotion(ref motion);
        return dy;
    }
}
