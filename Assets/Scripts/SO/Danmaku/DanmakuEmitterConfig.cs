using System;
using UnityEngine;

public enum EmitMode
{ 
    None,
    Line,
    Arc,
    /// <summary>波弹：扇形中心角随时间正弦摆动（东方常见的摆动 N-Way / 摆扇）。</summary>
    Wave,
    /// <summary>粒弹：锥形范围内随机方向与速度的霰弹散布。</summary>
    Grain,
}

public enum DanmakuSelectMode
{
    First,
    Sequential,
    Random
}

public enum EmitterCamp
{
    None,
    Player,
    Enemy
}

[Serializable]
public struct LineModeConfig
{
    public Vector2 lineDirection; // 替代 DirX/DirY
    [Min(1)] public int lineCount;
    [Min(0f)] public float lineSpacing;
}

[Serializable]
public struct ArcModeConfig
{
    public float arcStartAngle;    // 起始角度（度）
    public float arcAngle;      // 扇形展开角（度）
    public float arcRadius;        // 发射半径

    [Min(0f)]
    public int arcBulletCount;    // 弧线上子弹数
    public bool arcClockwise;
}

/// <summary>波弹：每齐射为一扇形，扇形中心角按正弦规律摆动。</summary>
[Serializable]
public struct WaveModeConfig
{
    [Tooltip("扇形中心基准角（度）；-90=正下，0=右")]
    public float centerAngleDeg;

    [Tooltip("中心角摆动半幅（度）")]
    [Min(0f)]
    public float swingDegrees;

    [Tooltip("摆动频率（Hz）")]
    [Min(0f)]
    public float swingHz;

    [Tooltip("摆动初相（度）")]
    public float phaseOffsetDeg;

    [Tooltip("每齐射的扇形展开角（度）")]
    [Min(0f)]
    public float spreadAngleDeg;

    [Min(1)]
    public int bulletCount;

    [Min(0f)]
    public float arcRadius;

    public bool clockwise;
}

/// <summary>粒弹：锥形内随机方向与速度的霰弹（东方妖精粒弹/撒弹）。</summary>
[Serializable]
public struct GrainModeConfig
{
    [Min(1)]
    public int bulletCount;

    [Tooltip("锥形中心方向（度）；-90=正下")]
    public float baseAngleDeg;

    [Tooltip("锥形半角（度）")]
    [Min(0f)]
    public float coneHalfAngleDeg;

    [Tooltip("相对 launchSpeed 的速度下限倍率（≤0 时烘焙使用 0.85）")]
    [Range(0.1f, 2f)]
    public float speedMinScale;

    [Tooltip("相对 launchSpeed 的速度上限倍率（≤0 时烘焙使用 1.15）")]
    [Range(0.1f, 2f)]
    public float speedMaxScale;

    [Tooltip("出生点相对发射点的随机散布半径（世界单位）")]
    [Min(0f)]
    public float spawnScatterRadius;

    public const float DefaultSpeedMinScale = 0.85f;
    public const float DefaultSpeedMaxScale = 1.15f;

    public static void NormalizeSpeedScales(ref GrainModeConfig grain)
    {
        if (grain.speedMinScale <= 0f)
            grain.speedMinScale = DefaultSpeedMinScale;
        if (grain.speedMaxScale <= 0f)
            grain.speedMaxScale = DefaultSpeedMaxScale;
        if (grain.speedMinScale > grain.speedMaxScale)
            (grain.speedMinScale, grain.speedMaxScale) = (grain.speedMaxScale, grain.speedMinScale);
    }

    public float ResolveSpeedMinScale() =>
        speedMinScale > 0f ? speedMinScale : DefaultSpeedMinScale;

    public float ResolveSpeedMaxScale() =>
        speedMaxScale > 0f ? speedMaxScale : DefaultSpeedMaxScale;
}


[CreateAssetMenu(fileName = "NewDanmakuEmitterConfig", menuName = "Configs/DanmakuEmitterConfig")]
public class DanmakuEmitterConfig : GameConfig, IReferenceResolver, ILogicTimingBake
{
    [Header("发射器预制体")]
    [Tooltip("池化 archetype Id（小写）；多条 EmitterConfig 可共用，displaySprite 在出池时应用。见 DanmakuEmitterPrefabArchetypes。")]
    public string emitterPrefabId;
    [NonSerialized]
    public int emitterPrefabIndex;

    [Header("装填弹幕配置")]
    [Tooltip("DanmakuConfig 的 ConfigId（小写）；Inspector 中从 Manifest / Configs/Danmaku 下拉选择")]
    public string[] danmakuConfigIds;
    [NonSerialized]
    public int[] danmakuCfgIndices;

    [Header("弹幕选择与发射模式")]
    public DanmakuSelectMode danmakuSelectMode = DanmakuSelectMode.First;
    public EmitMode emitMode = EmitMode.None;

    [Header("通用发射器参数")]

    [Min(0f)]
    [Tooltip("发射间隔（秒）；在 ILogicTimingBake.BakeLogicTiming 中烘焙为 launchCooldownFrames")]
    public float launchIntervalSeconds = 0.5f;

    [Tooltip("发射次数；-1 表示无限次数")]
    public int launchCount = -1;

    [NonSerialized] public int launchCooldownFrames;

    [Min(0f)]
    [Tooltip("弹幕初速度（世界单位/秒）；在 BakeLogicTiming 中烘焙为 launchSpeedPerFrame")]
    public float launchSpeed = 2f;

    [NonSerialized] public float launchSpeedPerFrame;

    [Header("编辑器显示")]
    [Tooltip("Scene / 配置预览中发射器本体的 Sprite；不参与战斗逻辑与烘焙")]
    public Sprite displaySprite;

    [Tooltip("发射器 Sprite 自转角速度（度/秒，>0 时持续自转；仅表现，不影响弹幕发射方向）")]
    public float displaySelfSpinDegreesPerSecond;

    [NonSerialized] public float displaySelfSpinRadPerFrame;

    [Tooltip("循环缩放倍数下限（相对预制体 Uniform 缩放；与上限相等或周期为 0 时不缩放）")]
    [Min(0.01f)]
    public float displayScaleMin = 1f;

    [Tooltip("循环缩放倍数上限（相对预制体 Uniform 缩放）")]
    [Min(0.01f)]
    public float displayScaleMax = 1f;

    [Tooltip("缩放循环频率（次/秒）；0 表示不循环缩放")]
    [Min(0f)]
    public float displayScaleCyclesPerSecond;

    [NonSerialized] public float displayScalePhaseRadPerFrame;

    [Tooltip("发射器位置偏移（相对于生成点），用于调整发射器位置")]
    public Vector2 emitterPosOffset = Vector2.zero;
    [Tooltip("发射器旋转偏移（度）；实例化为 CDanmakuEmitter 时烘焙为弧度 emitterRotOffsetRad，发射逻辑不再做 Deg2Rad")]
    public float emitterRotOffsetZ = 0;

    [Tooltip("弹幕生成时的旋转偏移（度）；烘焙为 danmakuRotOffsetRad")]
    public float danmakuRotOffsetZ = 90f;

    public EmitterCamp emitterCamp = EmitterCamp.Enemy;
    public AudioName audio_Fire = AudioName.None;

    [Header("Line Mode 参数")]
    public LineModeConfig lineModeConfig;

    [Header("Arc Mode 参数")]
    public ArcModeConfig arcModeConfig;

    [Header("Wave Mode 参数（波弹）")]
    public WaveModeConfig waveModeConfig;

    [Header("Grain Mode 参数（粒弹）")]
    public GrainModeConfig grainModeConfig;

    [NonSerialized] public float waveOmegaRadPerFrame;

#if UNITY_EDITOR
    void OnValidate()
    {
        if(!string.IsNullOrEmpty(emitterPrefabId))
            emitterPrefabId = StringHelper.NormalizeResourceId(emitterPrefabId);

        if(danmakuConfigIds != null)
        {
            for (int i = 0; i < danmakuConfigIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(danmakuConfigIds[i]))
                    danmakuConfigIds[i] = StringHelper.NormalizeResourceId(danmakuConfigIds[i]);
            }
        }

        if (displayScaleMin > displayScaleMax)
            (displayScaleMin, displayScaleMax) = (displayScaleMax, displayScaleMin);

        var grain = grainModeConfig;
        GrainModeConfig.NormalizeSpeedScales(ref grain);
        grainModeConfig = grain;
    }
#endif

    public void ResolveReferences(GameResDB resDb)
    {
        // 1. 解析发射器预制体索引
        emitterPrefabIndex = resDb.GetPrefabIndex(emitterPrefabId);
        if (emitterPrefabIndex == -1)
        {
            Logger.Warn(
                $"[DanmakuEmitterConfig] Prefab not found: '{emitterPrefabId}' " +
                $"(configId: {ConfigId})",
                LogTag.Resource
            );
        }

        // 2. 解析弹幕配置索引
        if (danmakuConfigIds != null && danmakuConfigIds.Length > 0)
        {
            danmakuCfgIndices = new int[danmakuConfigIds.Length];
            for (int i = 0; i < danmakuConfigIds.Length; i++)
            {
                danmakuCfgIndices[i] = resDb.GetConfigIndex(danmakuConfigIds[i]);
                if (danmakuCfgIndices[i] == -1)
                {
                    Logger.Warn(
                        $"[DanmakuEmitterConfig] Danmaku config not found: '{danmakuConfigIds[i]}' " +
                        $"(in emitter: {ConfigId})",
                        LogTag.Resource
                    );
                }
            }
        }
        else
        {
            danmakuCfgIndices = Array.Empty<int>();
        }
    }

    public void BakeLogicTiming(uint logicFPS)
    {
        float fps = Mathf.Max(1f, logicFPS);
        if (launchIntervalSeconds <= 0f)
            launchCooldownFrames = 0;
        else
            launchCooldownFrames = Mathf.Max(1, Mathf.RoundToInt(launchIntervalSeconds * fps));
        launchSpeedPerFrame = launchSpeed / fps;
        displaySelfSpinRadPerFrame = displaySelfSpinDegreesPerSecond * Mathf.Deg2Rad / fps;
        displayScalePhaseRadPerFrame = displayScaleCyclesPerSecond * Mathf.PI * 2f / fps;
        waveOmegaRadPerFrame = waveModeConfig.swingHz * Mathf.PI * 2f / fps;
    }
}
