using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 时间轴波次对「敌人死亡掉落」的覆盖方式（相对 <see cref="EnemyConfig.dropOnDeathCfgIndices"/>）。
/// </summary>
[Serializable]
public struct WaveSpawnQueueEntry
{
    [Tooltip("留空则使用波次的 enemyConfigId")]
    public string enemyConfigId;

    [Tooltip("阵型位置槽位；-1 表示与队列序号相同")]
    public int spawnSlotIndex;

    [Min(0f)]
    [Tooltip("与上一名敌人之间的延迟（秒）；≤0 时用波次 spawnIntervalSeconds")]
    public float delayAfterPreviousSeconds;
}

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

    [NonSerialized] public float defaultDescentSpeedPerFrame;

    [NonSerialized] public int spawnIntervalFrames;

    /// <summary>烘焙后的 <see cref="PathRouteMovementData"/> 在 <see cref="EnemyPathBakeCache"/> 中的索引。</summary>
    [NonSerialized] public int pathRouteBakeIndex = -1;

    public void BakeLogicTiming(uint logicFPS)
    {
        float fps = Mathf.Max(1f, logicFPS);
        startFrameOffset = startTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(startTimeSeconds * fps));
        defaultDescentSpeedPerFrame = defaultDescentSpeed / fps;
        spawnIntervalFrames = spawnIntervalSeconds <= 0f
            ? 0
            : Mathf.Max(1, Mathf.RoundToInt(spawnIntervalSeconds * fps));
        pathRouteBakeIndex = -1;
        movementData?.BakeMovementTiming(logicFPS);
    }

    /// <summary>将本波若为路径模式则注册到 <see cref="EnemyPathBakeCache"/>（时间轴 Begin 时调用）。</summary>
    public void BakePathRouteIfNeeded(uint logicFps)
    {
        pathRouteBakeIndex = -1;
        if (movementData is not PathRouteMovementData route)
            return;
        var baked = EnemyPathMovementBaking.BakeRoute(route, logicFps);
        pathRouteBakeIndex = EnemyPathBakeCache.Register(baked);
    }

    public int ResolveSpawnCount()
    {
        if (spawnQueue != null && spawnQueue.Length > 0)
            return spawnQueue.Length;
        return Mathf.Max(0, count);
    }

    public bool UsesSequentialSpawn =>
        (spawnQueue != null && spawnQueue.Length > 1)
        || (spawnSequentially && ResolveSpawnCount() > 1);

    [Tooltip("敌人配置引用")]
    public string enemyConfigId;

    [Tooltip("生成数量（当 spawnQueue 为空时生效）")]
    public int count = 1;

    [Header("出怪队列")]
    [Tooltip("启用后按间隔依次生成；若配置了 spawnQueue 则按其顺序与延迟")]
    public bool spawnSequentially;

    [Min(0f)]
    [Tooltip("依次生成时，相邻两名敌人的间隔（秒）；spawnQueue 条目未写 delay 时也用此默认值")]
    public float spawnIntervalSeconds = 0.35f;

    [Tooltip("显式出怪队列（非空时覆盖 count，按数组顺序生成）")]
    public WaveSpawnQueueEntry[] spawnQueue = Array.Empty<WaveSpawnQueueEntry>();

    [Tooltip("生成阵型 (Grid, Circle, Line, Random)")]
    public SpawnPattern spawnPattern = SpawnPattern.Line;
    [Tooltip("阵型展开范围（世界单位）；最终点限制在战斗区外、GO 回收边距内")]
    public Vector2 spawnAreaSize = new(10, 5);
    [Tooltip("相对上沿外侧刷怪锚点的偏移（锚点在战斗区顶边与回收顶边之间）")]
    public Vector2 spawnOffset = Vector2.zero;

    [SerializeReference]
    [MovementPatternSerialize]
    [Tooltip("本波敌人运动轨迹（东方系折线/贝塞尔/正圆等）；为空时可用下方默认下落")]
    public MovementPatternData movementData;

    [Tooltip("未配置 movementData 时是否使用默认竖直下落")]
    public bool useDefaultDescentIfNoMovement = true;

    [Min(0f)]
    [Tooltip("默认下落速度（世界单位/秒），仅当未配置 movementData 且上一项为真时生效")]
    public float defaultDescentSpeed = 3.6f;

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

        if (spawnQueue != null)
        {
            for (int i = 0; i < spawnQueue.Length; i++)
            {
                if (!string.IsNullOrEmpty(spawnQueue[i].enemyConfigId))
                    spawnQueue[i].enemyConfigId = spawnQueue[i].enemyConfigId.ToLowerInvariantTrimmed();
            }
        }
    }
#endif
}