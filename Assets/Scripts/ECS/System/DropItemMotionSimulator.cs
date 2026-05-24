/// <summary>
/// 掉落物竖直上抛运动积分（逻辑帧确定性，供 <see cref="DropItemSystem"/> 与编辑器预览共用）。
/// </summary>
public static class DropItemMotionSimulator
{
    public static CDropItemMotion CreateMotionFromConfig(DropItemConfig cfg, uint logicFps)
    {
        cfg.BakeLogicTiming(logicFps);
        return new CDropItemMotion
        {
            vyPerFrame = cfg.initialUpPerFrame,
            gravityPerFrame = cfg.gravityPerFrame,
            maxFallPerFrame = cfg.maxFallPerFrame,
        };
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
