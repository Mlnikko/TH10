using System;
using UnityEngine;

public enum EmitMode
{ 
    None,
    Line,
    Arc
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


[CreateAssetMenu(fileName = "NewDanmakuEmitterConfig", menuName = "Configs/DanmakuEmitterConfig")]
public class DanmakuEmitterConfig : GameConfig, IReferenceResolver, ILogicTimingBake
{
    [Header("发射器预制体")]
    [Tooltip("预制体 Id（小写）；Inspector 中从 GameResourceManifest / Prefabs/DanmakuEmitter 下拉选择")]
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
    }
}
