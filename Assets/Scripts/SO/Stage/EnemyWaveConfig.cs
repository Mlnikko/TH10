using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 时间轴波次对「敌人死亡掉落」的覆盖方式（相对 <see cref="EnemyConfig.dropOnDeathCfgIndices"/>）。
/// </summary>
public enum E_WaveDropOverrideMode : byte
{
    /// <summary>仅使用敌人配置上的掉落列表。</summary>
    UseEnemyConfig = 0,
    /// <summary>忽略敌人配置，仅使用本波次 <see cref="EnemyWaveConfig.waveDropOnDeathConfigIds"/>。</summary>
    Replace = 1,
    /// <summary>敌人配置与本波次列表合并掉落（先敌配置后波次）。</summary>
    Append = 2,
}

[CreateAssetMenu(fileName = "NewEnemyWave", menuName = "Configs/Stage/EnemyWaveConfig")]
public class EnemyWaveConfig : GameConfig, ILogicTimingBake
{
    [Tooltip("相对关卡开始的时刻（秒）；在 ILogicTimingBake.BakeLogicTiming 中烘焙为 startFrameOffset")]
    public float startTimeSeconds;

    [NonSerialized] public int startFrameOffset;

    public void BakeLogicTiming(uint logicFPS)
    {
        startFrameOffset = startTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(startTimeSeconds * logicFPS));
        movementData?.BakeMovementTiming(logicFPS);
    }

    [Tooltip("敌人配置引用")]
    public string enemyConfigId;

    [Tooltip("生成数量")]
    public int count = 1;

    [Tooltip("生成阵型 (Grid, Circle, Line, Random)")]
    public SpawnPattern spawnPattern = SpawnPattern.Line;
    public Vector2 spawnAreaSize = new(10, 5); // 生成区域大小
    public Vector2 spawnOffset = Vector2.zero; // 相对屏幕中心的偏移

    [SerializeReference]
    [MovementPatternSerialize]
    [Tooltip("本波敌人运动轨迹（东方系折线/贝塞尔/正圆等）；为空时可用下方默认下落")]
    public MovementPatternData movementData;

    [Tooltip("未配置 movementData 时是否使用默认竖直下落")]
    public bool useDefaultDescentIfNoMovement = true;

    [Min(0f)]
    [Tooltip("默认下落速度（世界单位 / 逻辑帧），仅当未配置 movementData 且上一项为真时生效")]
    public float defaultDescentSpeedPerFrame = 0.06f;

    [Tooltip("初始血量倍率 (用于难度调整)")]
    public float hpMultiplier = 1.0f;

    [Tooltip("是否等待此波次全灭后才继续后续逻辑 (仅用于特定脚本控制，时间线通常自动推进)")]
    public bool waitForClear = false;

    [Header("击杀掉落（时间轴）")]
    [Tooltip("相对敌人默认掉落的覆盖策略；由关卡时间轴 ResolveReferences 烘焙 waveDropOnDeathCfgIndices")]
    public E_WaveDropOverrideMode waveDropMode = E_WaveDropOverrideMode.UseEnemyConfig;

    [Tooltip("本波次掉落（DropItemConfig 的 ConfigId）；UseEnemyConfig 时忽略")]
    public string[] waveDropOnDeathConfigIds = Array.Empty<string>();

    [NonSerialized]
    public int[] waveDropOnDeathCfgIndices = Array.Empty<int>();

    /// <summary>由 <see cref="StageTimelineConfig.ResolveReferences"/> 调用。</summary>
    public void ResolveDropReferences(GameResDB resDb)
    {
        if (waveDropMode == E_WaveDropOverrideMode.UseEnemyConfig
            || waveDropOnDeathConfigIds == null
            || waveDropOnDeathConfigIds.Length == 0)
        {
            waveDropOnDeathCfgIndices = Array.Empty<int>();
            return;
        }

        var list = new List<int>(waveDropOnDeathConfigIds.Length);
        for (int i = 0; i < waveDropOnDeathConfigIds.Length; i++)
        {
            string id = waveDropOnDeathConfigIds[i];
            if (string.IsNullOrWhiteSpace(id))
                continue;
            int idx = resDb.GetConfigIndex(id.ToLowerInvariantTrimmed());
            if (idx >= 0)
                list.Add(idx);
            else
                Logger.Warn($"[EnemyWaveConfig] DropItemConfig not found: '{id}' (wave asset: {name})", LogTag.Resource);
        }
        waveDropOnDeathCfgIndices = list.ToArray();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        enemyConfigId = enemyConfigId.ToLowerInvariantTrimmed();
        if (waveDropOnDeathConfigIds != null)
        {
            for (int i = 0; i < waveDropOnDeathConfigIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(waveDropOnDeathConfigIds[i]))
                    waveDropOnDeathConfigIds[i] = waveDropOnDeathConfigIds[i].ToLowerInvariantTrimmed();
            }
        }
    }
#endif
}