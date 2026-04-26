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
    public float arcAngle;      // 弧度范围（度）
    public float arcRadius;        // 发射半径

    [Min(0f)]
    public int arcBulletCount;    // 弧线上子弹数
    public bool arcClockwise;
}


[CreateAssetMenu(fileName = "NewDanmakuEmitterConfig", menuName = "Configs/DanmakuEmitterConfig")]
public class DanmakuEmitterConfig : GameConfig, IReferenceResolver, ILogicTimingBake
{
    [Header("发射器预制体")]
    public string emitterPrefabId;
    [NonSerialized]
    public int emitterPrefabIndex;

    [Header("装填弹幕配置")]
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

    [NonSerialized] public int launchCooldownFrames;

    [Min(0f)]
    public float launchSpeed = 2f;

    [Tooltip("发射器位置偏移（相对于生成点），用于调整发射器位置")]
    public Vector2 emitterPosOffset = Vector2.zero;
    [Tooltip("发射器旋转偏移（度），用于调整发射器朝向")]
    public float emitterRotOffsetZ = 0;

    [Tooltip("弹幕发射时的旋转偏移（度），用于调整弹幕朝向")]
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
            emitterPrefabId = emitterPrefabId.ToLowerInvariant().Trim();

        if(danmakuConfigIds != null)
        {
            for (int i = 0; i < danmakuConfigIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(danmakuConfigIds[i]))
                    danmakuConfigIds[i] = danmakuConfigIds[i].ToLowerInvariant().Trim();
            }
        }
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
        if (launchIntervalSeconds <= 0f)
            launchCooldownFrames = 0;
        else
            launchCooldownFrames = Mathf.Max(1, Mathf.RoundToInt(launchIntervalSeconds * logicFPS));
    }
}
