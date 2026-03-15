using UnityEngine;

public enum EmitterCamp
{
    None,
    Player,
    Enemy
}

public class DanmakuEmitterConfigViewer : MonoBehaviour
{
    [Header("配置文件")]
    public DanmakuEmitterConfig emitterConfig;
    public DanmakuConfig danmakuConfig;

    [Header("发射器类型")]
    [SerializeField] EmitMode emitterType;

    [Header("发射器阵营")]
    [SerializeField] EmitterCamp emitterCamp;

    [Header("发射器Offset调整")]
    [SerializeField] Vector2 emitPosOffset;
    [SerializeField] float emitRotOffsetZ;

    [Header("装填弹幕旋转")]
    [SerializeField] float danmakuRotOffsetZ;

    [Header("Line Mode 参数")]
    [SerializeField] LineModeConfig lineModeConfig;

    [Header("Arc Mode 参数")]
    [SerializeField] ArcModeConfig arcModeConfig;

    [Header("发射音效")]
    [SerializeField] AudioName launchAudio;

    [Header("发射间隔")]
    [SerializeField] float launchInterval;

    [Header("发射速度")]
    [SerializeField] float launchSpeed;

    public bool LoadEmitterConfig()
    {
        if (emitterConfig == null)
        {
            Logger.Warn("发射器配置为空，无法加载", LogTag.Config);
            return false;
        }

        emitterType = emitterConfig.emitMode;
        emitterCamp = emitterConfig.emitterCamp;
        emitPosOffset = emitterConfig.emitterPosOffset;
        emitRotOffsetZ = emitterConfig.emitterRotOffsetZ;

        danmakuRotOffsetZ = emitterConfig.danmakuRotOffsetZ;

        lineModeConfig = emitterConfig.lineModeConfig;
        arcModeConfig = emitterConfig.arcModeConfig;

        launchInterval = emitterConfig.launchInterval;
        launchSpeed = emitterConfig.launchSpeed;
        launchAudio = emitterConfig.audio_Fire;
        

        Logger.Debug("已加载发射器配置" + emitterConfig.name, LogTag.Config);

        return true;
    }

    public void SaveEmitterConfig()
    {
        if (emitterConfig == null)
        {
            Logger.Warn("发射器配置为空，无法保存", LogTag.Config);
            return;
        }

        emitterConfig.emitMode = emitterType;
        emitterConfig.emitterCamp = emitterCamp;
        emitterConfig.emitterPosOffset = emitPosOffset;
        emitterConfig.emitterRotOffsetZ = emitRotOffsetZ;

        emitterConfig.danmakuRotOffsetZ = danmakuRotOffsetZ;

        emitterConfig.lineModeConfig = lineModeConfig;
        emitterConfig.arcModeConfig = arcModeConfig;

        emitterConfig.launchInterval = launchInterval;
        emitterConfig.launchSpeed = launchSpeed;
        emitterConfig.audio_Fire = launchAudio;
      
        Logger.Debug("成功保存发射器配置" + emitterConfig.name, LogTag.Config);
    }

    public void PreviewEmitterEffect()
    {
        LoadEmitterConfig();
    }

    void EmitPreviewDanmaku()
    {
      
    }
}
