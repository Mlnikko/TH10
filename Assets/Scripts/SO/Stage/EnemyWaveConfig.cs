using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 时间轴波次对「敌人死亡掉落」的覆盖方式（相对 <see cref="EnemyConfig.dropOnDeathBaked"/>）。
/// </summary>
/// <summary>波次内敌人运动路径的分配方式。</summary>
public enum E_WavePathAssignment : byte
{
    /// <summary>全部队列条目共用 <see cref="EnemyWaveConfig.pathRoute"/>。</summary>
    Shared = 0,
    /// <summary>各条目可使用 <see cref="WaveSpawnQueueEntry.pathRouteOverride"/>，未配置时回退到波次 pathRoute。</summary>
    PerQueueEntry = 1,
}

[Serializable]
public struct WaveSpawnQueueEntry
{
    [Tooltip("敌人配置 id（EnemyConfig 的 ConfigId）")]
    public string enemyConfigId;

    [Tooltip("阵型位置槽位；-1 表示与队列序号相同")]
    public int spawnSlotIndex;

    [Min(0f)]
    [Tooltip("与上一名敌人之间的延迟（秒）；≤0 时用波次 spawnIntervalSeconds")]
    public float delayAfterPreviousSeconds;

    [Tooltip("独立运动路径（仅 pathAssignment=PerQueueEntry 时生效；为空则使用波次 pathRoute）")]
    public PathRouteMovementData pathRouteOverride;
}

public enum E_WaveDropOverrideMode : byte
{
    /// <summary>仅使用敌人配置上的掉落列表。</summary>
    UseEnemyConfig = 0,
    /// <summary>忽略敌人配置，仅使用本波次掉落列表。</summary>
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

    /// <summary>烘焙后的 <see cref="PathRouteMovementData"/> 在 <see cref="EnemyPathBakeCache"/> 中的索引（Shared 模式或回退）。</summary>
    [NonSerialized] public int pathRouteBakeIndex = -1;

    /// <summary>PerQueueEntry 模式下各队列条目的路径烘焙索引。</summary>
    [NonSerialized] public int[] spawnQueuePathBakeIndices;

    [SerializeField, HideInInspector] string enemyConfigId;
    [SerializeField, HideInInspector] int count = 1;

    public void BakeLogicTiming(uint logicFPS)
    {
        EnsureSpawnQueueMigrated();
        float fps = Mathf.Max(1f, logicFPS);
        startFrameOffset = startTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(startTimeSeconds * fps));
        defaultDescentSpeedPerFrame = defaultDescentSpeed / fps;
        spawnIntervalFrames = spawnIntervalSeconds <= 0f
            ? 0
            : Mathf.Max(1, Mathf.RoundToInt(spawnIntervalSeconds * fps));
        pathRouteBakeIndex = -1;
        pathRoute?.BakeMovementTiming(logicFPS);
    }

    [Tooltip("路径分配：统一路径或队列条目可单独覆盖")]
    public E_WavePathAssignment pathAssignment = E_WavePathAssignment.Shared;

    public bool UsesPerQueueEntryPaths => pathAssignment == E_WavePathAssignment.PerQueueEntry;

    /// <summary>Scene 路径预览/编辑锚定的出怪队列条目（Shared=生成点锚定，PerQueueEntry=该条目路径）。</summary>
    public int ResolvePathDisplayEntryIndex(int requestedEntryIndex)
    {
        EnsureSpawnQueueMigrated();
        int count = ResolveSpawnCount();
        if (count <= 0)
            return 0;

        return Mathf.Clamp(requestedEntryIndex, 0, count - 1);
    }

    /// <summary>将本波路径注册到 <see cref="EnemyPathBakeCache"/>（时间轴 Begin 时调用）。</summary>
    public void BakePathRouteIfNeeded(uint logicFps)
    {
        pathRouteBakeIndex = RegisterPathRoute(pathRoute, logicFps);
        spawnQueuePathBakeIndices = null;

        if (pathAssignment != E_WavePathAssignment.PerQueueEntry)
            return;

        EnsureSpawnQueueMigrated();
        int n = spawnQueue != null ? spawnQueue.Length : 0;
        if (n <= 0)
            return;

        spawnQueuePathBakeIndices = new int[n];
        for (int i = 0; i < n; i++)
            spawnQueuePathBakeIndices[i] = RegisterPathRoute(ResolveEffectivePathRoute(i), logicFps);
    }

    public static bool HasUsablePathRoute(PathRouteMovementData route) =>
        PathRouteMovementData.HasAnyPathContent(route);

    /// <summary>解析队列条目实际使用的路径数据（编辑器 Gizmo / 运行时运动一致）。</summary>
    public PathRouteMovementData ResolvePathForEntry(int entryIndex)
    {
        if (pathAssignment == E_WavePathAssignment.PerQueueEntry
            && spawnQueue != null
            && entryIndex >= 0
            && entryIndex < spawnQueue.Length)
        {
            var entryOverride = spawnQueue[entryIndex].pathRouteOverride;
            if (HasUsablePathRoute(entryOverride))
                return entryOverride;
        }

        return HasUsablePathRoute(pathRoute) ? pathRoute : null;
    }

    /// <summary>
    /// 烘焙 / Scene 轨迹预览用：PerQueueEntry 且条目未配全 legs 时，继承波次 pathRoute 的路段曲线配置。
    /// </summary>
    public PathRouteMovementData ResolveEffectivePathRoute(int entryIndex)
    {
        var route = ResolvePathForEntry(entryIndex);
        if (route == null)
            return null;

        if (pathAssignment != E_WavePathAssignment.PerQueueEntry
            || !HasUsablePathRoute(pathRoute)
            || route == pathRoute)
            return route;

        return PathRouteMovementData.MergeLegsFromSharedFallback(route, pathRoute);
    }

    /// <summary>Scene / Inspector 编辑：Shared 改波次 pathRoute；PerQueueEntry 改条目 override（无则回退只读解析）。</summary>
    public PathRouteMovementData ResolveEditablePathRoute(int entryIndex)
    {
        if (pathAssignment == E_WavePathAssignment.PerQueueEntry
            && spawnQueue != null
            && entryIndex >= 0
            && entryIndex < spawnQueue.Length
            && HasUsablePathRoute(spawnQueue[entryIndex].pathRouteOverride))
            return spawnQueue[entryIndex].pathRouteOverride;

        return pathRoute;
    }

#if UNITY_EDITOR
    /// <summary>PerQueueEntry：为条目创建可编辑的 pathRouteOverride（从波次 pathRoute 复制；无则建默认）。</summary>
    public void EnsureEntryPathOverrideInitialized(int entryIndex)
    {
        if (pathAssignment != E_WavePathAssignment.PerQueueEntry)
            return;

        EnsureSpawnQueueMigrated();
        if (spawnQueue == null || entryIndex < 0 || entryIndex >= spawnQueue.Length)
            return;

        var entry = spawnQueue[entryIndex];
        if (HasUsablePathRoute(entry.pathRouteOverride))
            return;

        entry.pathRouteOverride = HasUsablePathRoute(pathRoute)
            ? PathRouteMovementData.Duplicate(pathRoute)
            : PathRouteMovementData.CreateLinearDown(48f, defaultDescentSpeed);
        spawnQueue[entryIndex] = entry;
    }
#endif

    public int ResolvePathBakeIndex(int entryIndex)
    {
        if (pathAssignment == E_WavePathAssignment.PerQueueEntry
            && spawnQueuePathBakeIndices != null
            && entryIndex >= 0
            && entryIndex < spawnQueuePathBakeIndices.Length)
            return spawnQueuePathBakeIndices[entryIndex];

        return pathRouteBakeIndex;
    }

    static int RegisterPathRoute(PathRouteMovementData route, uint logicFps)
    {
        if (!HasUsablePathRoute(route))
            return -1;
        var baked = EnemyPathMovementBaking.BakeRoute(route, logicFps);
        return EnemyPathBakeCache.Register(baked);
    }

    public int ResolveSpawnCount()
    {
        EnsureSpawnQueueMigrated();
        return spawnQueue != null ? spawnQueue.Length : 0;
    }

    public bool UsesSequentialSpawn =>
        spawnSequentially && ResolveSpawnCount() > 1;

    [Header("出怪队列")]
    [Tooltip("本波生成的敌人列表（种类、槽位、顺序延迟）")]
    public WaveSpawnQueueEntry[] spawnQueue = Array.Empty<WaveSpawnQueueEntry>();

    [Tooltip("队列内多于 1 名敌人时，是否按间隔依次生成（否则同帧全部生成）")]
    public bool spawnSequentially;

    [Min(0f)]
    [Tooltip("依次生成时，相邻两名敌人的默认间隔（秒）；队列条目未写 delay 时用此值")]
    public float spawnIntervalSeconds = 0.35f;

    [Tooltip("生成阵型 (Grid, Circle, Line, Random)")]
    public SpawnPattern spawnPattern = SpawnPattern.Line;
    [Tooltip("阵型展开范围（世界单位）；最终点限制在战斗区外、GO 回收边距内")]
    public Vector2 spawnAreaSize = new(10, 5);
    [Tooltip("相对上沿外侧刷怪锚点的偏移（锚点在战斗区顶边与回收顶边之间）")]
    public Vector2 spawnOffset = Vector2.zero;

    [Tooltip("运动路径（生成点为起点；路径点 + 分段曲线）；为空时可用下方默认下落")]
    public PathRouteMovementData pathRoute;

    [Tooltip("未配置 pathRoute 时是否使用默认竖直下落")]
    public bool useDefaultDescentIfNoMovement = true;

    [Min(0f)]
    [Tooltip("默认下落速度（世界单位/秒），仅当未配置 pathRoute 且上一项为真时生效")]
    public float defaultDescentSpeed = 3.6f;

    [Tooltip("初始血量倍率 (用于难度调整)")]
    public float hpMultiplier = 1.0f;

    [Tooltip("是否等待此波次全灭后才继续后续逻辑 (仅用于特定脚本控制，时间线通常自动推进)")]
    public bool waitForClear = false;

    [Header("击杀掉落（时间轴）")]
    [Tooltip("相对敌人默认掉落的覆盖策略；由关卡时间轴 ResolveReferences 烘焙 waveDropOnDeathBaked")]
    public E_WaveDropOverrideMode waveDropMode = E_WaveDropOverrideMode.UseEnemyConfig;

    [Tooltip("本波次掉落种类与数量；UseEnemyConfig 时忽略")]
    public DeathDropEntry[] waveDropOnDeathEntries = Array.Empty<DeathDropEntry>();

    [NonSerialized] public BakedDeathDropEntry[] waveDropOnDeathBaked = Array.Empty<BakedDeathDropEntry>();

    [SerializeField, HideInInspector] string[] waveDropOnDeathConfigIds;

    /// <summary>由 <see cref="StageTimelineConfig.ResolveReferences"/> 调用。</summary>
    public void ResolveDropReferences(GameResDB resDb)
    {
        if (waveDropMode == E_WaveDropOverrideMode.UseEnemyConfig)
        {
            waveDropOnDeathBaked = Array.Empty<BakedDeathDropEntry>();
            return;
        }

        EnsureWaveDropEntriesMigrated();
        if (waveDropOnDeathEntries == null || waveDropOnDeathEntries.Length == 0)
        {
            waveDropOnDeathBaked = Array.Empty<BakedDeathDropEntry>();
            return;
        }

        waveDropOnDeathBaked = DeathDropBaking.BakeEntries(
            waveDropOnDeathEntries, resDb, $"EnemyWaveConfig {name}");
    }

    public void EnsureSpawnQueueMigrated()
    {
        if (spawnQueue != null && spawnQueue.Length > 0)
            return;

        if (string.IsNullOrWhiteSpace(enemyConfigId))
        {
            spawnQueue = Array.Empty<WaveSpawnQueueEntry>();
            return;
        }

        int n = Mathf.Max(1, count);
        spawnQueue = new WaveSpawnQueueEntry[n];
        string id = enemyConfigId.ToLowerInvariantTrimmed();
        for (int i = 0; i < n; i++)
        {
            spawnQueue[i] = new WaveSpawnQueueEntry
            {
                enemyConfigId = id,
                spawnSlotIndex = -1,
                delayAfterPreviousSeconds = 0f,
            };
        }
    }

    void EnsureWaveDropEntriesMigrated()
    {
        if (waveDropOnDeathEntries != null && waveDropOnDeathEntries.Length > 0)
            return;
        if (waveDropOnDeathConfigIds == null || waveDropOnDeathConfigIds.Length == 0)
            return;

        waveDropOnDeathEntries = new DeathDropEntry[waveDropOnDeathConfigIds.Length];
        for (int i = 0; i < waveDropOnDeathConfigIds.Length; i++)
        {
            waveDropOnDeathEntries[i] = new DeathDropEntry
            {
                dropConfigId = waveDropOnDeathConfigIds[i],
                count = 1,
            };
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureSpawnQueueMigrated();
        pathRoute?.EnsureSpawnAnchoredFormat();
        if (spawnQueue != null)
        {
            for (int i = 0; i < spawnQueue.Length; i++)
                spawnQueue[i].pathRouteOverride?.EnsureSpawnAnchoredFormat();
        }

        EnsureWaveDropEntriesMigrated();
        DeathDropBaking.NormalizeEntries(waveDropOnDeathEntries);

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
