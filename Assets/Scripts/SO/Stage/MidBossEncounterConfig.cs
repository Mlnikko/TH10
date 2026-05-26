using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中场 Boss 遭遇配置：登场/在场/退场时机、三段路径、战斗属性与表现覆盖。
/// 由 <see cref="StageTimelineConfig"/> 引用；运行时由 <see cref="CMidBossEncounter"/> + <see cref="MidBossEncounterSystem"/> 驱动。
/// </summary>
[CreateAssetMenu(fileName = "MidBossEncounter", menuName = "Configs/Stage/Mid Boss Encounter")]
public class MidBossEncounterConfig : GameConfig, ILogicTimingBake, IReferenceResolver
{
    [Tooltip("是否启用本场中场 Boss")]
    public bool enabled = true;

    [Header("时机")]
    [Tooltip("相对关卡开始的登场时刻（秒）")]
    public float spawnTimeSeconds = 50f;

    [Tooltip("入场路径播完后，在场战斗/循环移动的持续时间（秒）")]
    public float onFieldDurationSeconds = 45f;

    [NonSerialized] public int spawnFrameOffset;
    [NonSerialized] public int onFieldDurationFrames;
    [NonSerialized] public int entryDurationFrames;
    [NonSerialized] public int exitDurationFrames;

    [Header("敌人")]
    [Tooltip("敌人配置 id（EnemyConfig 的 ConfigId）")]
    public string enemyConfigId;

    [Tooltip("相对战斗区中心的位移")]
    public Vector2 spawnOffset;

    [Tooltip("相对战斗区高度的 Y 归一化偏移")]
    [Range(-0.5f, 0.5f)]
    public float yHeightNorm = 0.25f;

    [Header("轨迹")]
    [Tooltip("入场路径；为空则登场点直接开始场内循环")]
    public PathRouteMovementData entryPathRoute;

    [Tooltip("场内循环路径；为空则在场阶段静止")]
    public PathRouteMovementData loopPathRoute;

    [Tooltip("退场路径；为空则在场时间结束后于当前位置回收")]
    public PathRouteMovementData exitPathRoute;

    [NonSerialized] public int entryPathRouteBakeIndex = -1;
    [NonSerialized] public int loopPathRouteBakeIndex = -1;
    [NonSerialized] public int exitPathRouteBakeIndex = -1;

    [Header("战斗属性覆盖")]
    [Tooltip("≤0 时使用 EnemyConfig.maxHealth")]
    public int maxHealthOverride;

    [Tooltip("留空则使用 EnemyConfig.emitterConfigId")]
    public string emitterConfigIdOverride;

    [NonSerialized] public int emitterConfigIndexOverride = -1;

    [Header("击杀掉落")]
    public E_WaveDropOverrideMode dropOverrideMode = E_WaveDropOverrideMode.UseEnemyConfig;

    [Tooltip("相对敌人默认掉落的覆盖；UseEnemyConfig 时忽略")]
    public DeathDropEntry[] dropOnDeathEntries = Array.Empty<DeathDropEntry>();

    [NonSerialized] public BakedDeathDropEntry[] dropOnDeathBaked = Array.Empty<BakedDeathDropEntry>();

    [SerializeField, HideInInspector] string[] dropOnDeathConfigIds;

    [Header("Animator")]
    [Tooltip("入场：Animator 状态名（非 AnimationClip）；留空则该阶段不 Play")]
    public string animatorStateEntry = "Enter";

    [Tooltip("场内循环：Animator 状态名")]
    public string animatorStateLoop = "Loop";

    [Tooltip("退场：Animator 状态名")]
    public string animatorStateExit = "Exit";

    [Tooltip("回退默认：Animator 状态名")]
    public string animatorStateIdle = "Idle";

    [Tooltip("移动表现（可选，当前逻辑未使用）：Animator 状态名")]
    public string animatorStateMove = "Move";

    public void BakeLogicTiming(uint logicFPS)
    {
        float fps = Mathf.Max(1f, logicFPS);
        spawnFrameOffset = spawnTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(spawnTimeSeconds * fps));
        onFieldDurationFrames = onFieldDurationSeconds <= 0f
            ? 0
            : Mathf.Max(0, Mathf.RoundToInt(onFieldDurationSeconds * fps));

        entryPathRoute?.BakeMovementTiming(logicFPS);
        loopPathRoute?.BakeMovementTiming(logicFPS);
        exitPathRoute?.BakeMovementTiming(logicFPS);

        entryDurationFrames = 0;
        exitDurationFrames = 0;
        entryPathRouteBakeIndex = -1;
        loopPathRouteBakeIndex = -1;
        exitPathRouteBakeIndex = -1;
    }

    /// <summary>将三段路径注册到 <see cref="EnemyPathBakeCache"/>（时间轴 Begin 时调用）。</summary>
    public void BakePathRoutesIfNeeded(uint logicFps)
    {
        entryPathRouteBakeIndex = BakeRouteIndex(entryPathRoute, logicFps, out entryDurationFrames);
        loopPathRouteBakeIndex = BakeRouteIndex(loopPathRoute, logicFps, out _);
        exitPathRouteBakeIndex = BakeRouteIndex(exitPathRoute, logicFps, out exitDurationFrames);
    }

    static int BakeRouteIndex(PathRouteMovementData route, uint logicFps, out int durationFrames)
    {
        durationFrames = 0;
        if (route == null)
            return -1;

        var baked = EnemyPathMovementBaking.BakeRoute(route, logicFps);
        durationFrames = baked.durationFrames > 0 ? baked.durationFrames : 0;
        return EnemyPathBakeCache.Register(baked);
    }

    public void ResolveReferences(GameResDB resDb)
    {
        emitterConfigIndexOverride = -1;
        if (!string.IsNullOrWhiteSpace(emitterConfigIdOverride))
        {
            emitterConfigIndexOverride = resDb.GetConfigIndex(emitterConfigIdOverride.ToLowerInvariantTrimmed());
            if (emitterConfigIndexOverride < 0)
            {
                Logger.Warn(
                    $"[MidBossEncounter] Emitter config not found: '{emitterConfigIdOverride}' ({ConfigId})",
                    LogTag.Resource);
            }
        }

        if (dropOverrideMode == E_WaveDropOverrideMode.UseEnemyConfig)
        {
            dropOnDeathBaked = Array.Empty<BakedDeathDropEntry>();
            return;
        }

        EnsureDropEntriesMigrated();
        if (dropOnDeathEntries == null || dropOnDeathEntries.Length == 0)
        {
            dropOnDeathBaked = Array.Empty<BakedDeathDropEntry>();
            return;
        }

        dropOnDeathBaked = DeathDropBaking.BakeEntries(
            dropOnDeathEntries, resDb, $"MidBossEncounter {ConfigId}");
    }

    void EnsureDropEntriesMigrated()
    {
        if (dropOnDeathEntries != null && dropOnDeathEntries.Length > 0)
            return;
        if (dropOnDeathConfigIds == null || dropOnDeathConfigIds.Length == 0)
            return;

        dropOnDeathEntries = new DeathDropEntry[dropOnDeathConfigIds.Length];
        for (int i = 0; i < dropOnDeathConfigIds.Length; i++)
        {
            dropOnDeathEntries[i] = new DeathDropEntry
            {
                dropConfigId = dropOnDeathConfigIds[i],
                count = 1,
            };
        }
    }

    public float ResolveHpMultiplier(EnemyConfig enemyCfg)
    {
        if (maxHealthOverride <= 0 || enemyCfg == null || enemyCfg.maxHealth <= 0)
            return 1f;
        return maxHealthOverride / (float)enemyCfg.maxHealth;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        enemyConfigId = enemyConfigId.ToLowerInvariantTrimmed();
        emitterConfigIdOverride = emitterConfigIdOverride.ToLowerInvariantTrimmed();
        EnsureDropEntriesMigrated();
        DeathDropBaking.NormalizeEntries(dropOnDeathEntries);
    }
#endif
}
