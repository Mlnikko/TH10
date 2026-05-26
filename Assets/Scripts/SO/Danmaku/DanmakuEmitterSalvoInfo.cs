using UnityEngine;

/// <summary>
/// 发射器齐射弹数与发射次数的只读统计（编辑器校验与预览摘要共用）。
/// </summary>
public static class DanmakuEmitterSalvoInfo
{
    /// <summary>将配置中的 launchCount 烘焙为 ECS 用的 launchCountMax；-1 无限，0 视为误填并当作无限。</summary>
    public static int NormalizeLaunchCountMax(int launchCount) =>
        launchCount == 0 ? -1 : launchCount;

    /// <summary>烘焙后组件上，当前模式单次齐射的弹幕数量。</summary>
    public static int GetSalvoBulletCount(in CDanmakuEmitter emitter)
    {
        switch (emitter.emitMode)
        {
            case EmitMode.Line:
                return emitter.lineCount;
            case EmitMode.Arc:
            case EmitMode.Wave:
                return emitter.arcBulletCount;
            case EmitMode.Grain:
                return emitter.grainBulletCount;
            default:
                return 0;
        }
    }

    /// <summary>当前发射模式下，单次齐射生成的弹幕数量；None 或无效配置返回 0。</summary>
    public static int GetSalvoBulletCount(in DanmakuEmitterConfig config)
    {
        if (config == null)
            return 0;

        switch (config.emitMode)
        {
            case EmitMode.Line:
                return Mathf.Max(0, config.lineModeConfig.lineCount);
            case EmitMode.Arc:
                return Mathf.Max(0, config.arcModeConfig.arcBulletCount);
            case EmitMode.Wave:
                return Mathf.Max(0, config.waveModeConfig.bulletCount);
            case EmitMode.Grain:
                return Mathf.Max(0, config.grainModeConfig.bulletCount);
            default:
                return 0;
        }
    }

    public static string FormatLaunchCountLabel(int launchCount)
    {
        if (launchCount < 0)
            return "齐射次数：无限";
        if (launchCount == 0)
            return "齐射次数：0（不会发射，保存时将自动改为无限）";
        return $"齐射次数：{launchCount}";
    }

    /// <summary>若当前配置无法产生弹幕，返回原因文案。</summary>
    public static bool TryGetSalvoIssue(in DanmakuEmitterConfig config, out string message)
    {
        message = null;
        if (config == null)
        {
            message = "配置为空。";
            return true;
        }

        if (config.emitMode == EmitMode.None)
        {
            message = "发射模式为 None，不会发射弹幕。";
            return true;
        }

        if (config.danmakuConfigIds == null || config.danmakuConfigIds.Length == 0)
        {
            message = "未装填 danmakuConfigIds。";
            return true;
        }

        int salvo = GetSalvoBulletCount(in config);
        if (salvo <= 0)
        {
            message = $"当前 {config.emitMode} 模式的每齐射弹数为 {salvo}，请检查 lineCount / arcBulletCount / bulletCount。";
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    public static void ClampActiveModeSalvoCounts(DanmakuEmitterConfig config)
    {
        if (config == null)
            return;

        switch (config.emitMode)
        {
            case EmitMode.Line:
            {
                var line = config.lineModeConfig;
                if (line.lineCount < 1)
                {
                    line.lineCount = 1;
                    config.lineModeConfig = line;
                }

                break;
            }
            case EmitMode.Arc:
            {
                var arc = config.arcModeConfig;
                if (arc.arcBulletCount < 1)
                {
                    arc.arcBulletCount = 1;
                    config.arcModeConfig = arc;
                }

                break;
            }
            case EmitMode.Wave:
            {
                var wave = config.waveModeConfig;
                if (wave.bulletCount < 1)
                {
                    wave.bulletCount = 1;
                    config.waveModeConfig = wave;
                }

                break;
            }
            case EmitMode.Grain:
            {
                var grain = config.grainModeConfig;
                if (grain.bulletCount < 1)
                {
                    grain.bulletCount = 1;
                    config.grainModeConfig = grain;
                }

                break;
            }
        }
    }
#endif
}
