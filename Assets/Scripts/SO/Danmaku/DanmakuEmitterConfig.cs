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


[CreateAssetMenu(fileName = "NewDanmakuEmitterConfig", menuName = "Configs/DanmakuEmitterConfig")]
public class DanmakuEmitterConfig : GameConfig, IReferenceResolver
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

    //public int minPoolSize = 100;
    //public int maxPoolSize = 500;

    [Header("通用发射器参数")]

    [Min(0f)] public float launchInterval = 0.5f;
    public float launchSpeed = 2f;

    public Vector2 launchPosOffset = Vector2.zero;
    public Vector3 launchRotOffset = Vector3.zero;

    public EmitterCamp emitterCamp = EmitterCamp.Enemy;
    public AudioName audio_Fire = AudioName.None;

    [Header("Line 发射器")]

    [Tooltip("发射方向，会转换成单位向量使用")]
    public Vector2 LineDirection = Vector2.up; // 替代 DirX/DirY
    [Min(1)] public int LineCount = 1;
    [Min(0f)] public float LineSpacing = 0.2f;


    [Header("Arc 发射器")]

    public float ArcAngle = 90f;      // 弧度范围（度）

    [Min(0f)]
    public int ArcBulletCount = 5;    // 弧线上子弹数
    public bool ArcClockwise = true;

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

    public void ResolveReferences(GameResDB resDb)
    {
        // 1. 解析发射器预制体索引
        emitterPrefabIndex = resDb.GetPrefabIndex(emitterPrefabId);
        if (emitterPrefabIndex == -1)
        {
            Logger.Warn(
                $"[DanmakuEmitterConfig] Prefab not found: '{emitterPrefabId}' " +
                $"(configId: {configId})",
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
                        $"(in emitter: {configId})",
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
}
