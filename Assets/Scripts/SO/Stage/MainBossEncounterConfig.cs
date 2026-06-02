using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关底 Boss 单独配置资产（登场时机、符卡阶段列表等）。由 <see cref="StageTimelineConfig"/> 引用。
/// </summary>
[CreateAssetMenu(fileName = "MainBossEncounter", menuName = "Configs/Stage/Main Boss Encounter")]
public class MainBossEncounterConfig : GameConfig, ILogicTimingBake, IReferenceResolver
{
    [Tooltip("是否启用关底 Boss")]
    public bool enabled = true;

    [Tooltip("相对关卡开始的登场时刻（秒）；在 BakeLogicTiming 中烘焙为 spawnFrameOffset")]
    public float spawnTimeSeconds = 120f;

    [NonSerialized] public int spawnFrameOffset;

    [Tooltip("敌人配置 id（EnemyConfig 的 ConfigId）")]
    public string enemyConfigId;

    [Tooltip("相对战斗区中心的位移")]
    public Vector2 spawnOffset;

    [Tooltip("相对战斗区高度的 Y 归一化偏移")]
    [Range(-0.5f, 0.5f)]
    public float yHeightNorm = 0.2f;

    [Header("轨迹")]
    [Tooltip("登场路径；为空则直接在登场点出现")]
    public PathRouteMovementData entryPathRoute;

    [Tooltip("符卡战阶段循环路径；为空则在场阶段静止")]
    public PathRouteMovementData loopPathRoute;

    [NonSerialized] public int entryPathRouteBakeIndex = -1;
    [NonSerialized] public int loopPathRouteBakeIndex = -1;
    [NonSerialized] public int entryDurationFrames;

    [Tooltip("BOSS 登场后的对话/无敌时间（秒）；加载时烘焙为 bossIntroDurationFrames")]
    public float bossIntroDurationSeconds = 3f;

    [NonSerialized] public int bossIntroDurationFrames;

    [Header("符卡阶段")]
    [Tooltip("BOSS 阶段 / 符卡（独立 BossPhase 资产）")]
    public List<BossPhaseConfig> bossPhases = new();

    public void ResolveReferences(GameResDB resDb)
    {
        if (bossPhases == null)
            return;

        for (int i = 0; i < bossPhases.Count; i++)
        {
            if (bossPhases[i] is IReferenceResolver resolver)
                resolver.ResolveReferences(resDb);
        }
    }

    public void BakeLogicTiming(uint logicFPS)
    {
        spawnFrameOffset = spawnTimeSeconds <= 0f ? 0 : Mathf.Max(0, Mathf.RoundToInt(spawnTimeSeconds * logicFPS));
        bossIntroDurationFrames = bossIntroDurationSeconds <= 0f
            ? 0
            : Mathf.Max(0, Mathf.RoundToInt(bossIntroDurationSeconds * logicFPS));

        entryPathRoute?.BakeMovementTiming(logicFPS);
        loopPathRoute?.BakeMovementTiming(logicFPS);

        if (bossPhases == null)
            return;
        for (int i = 0; i < bossPhases.Count; i++)
        {
            if (bossPhases[i] is ILogicTimingBake phaseBake)
                phaseBake.BakeLogicTiming(logicFPS);
        }
    }

    /// <summary>将路径注册到 <see cref="EnemyPathBakeCache"/>（时间轴 Begin 时调用）。</summary>
    public void BakePathRoutesIfNeeded(uint logicFps)
    {
        entryPathRouteBakeIndex = BakeRouteIndex(entryPathRoute, logicFps, out entryDurationFrames);
        loopPathRouteBakeIndex = BakeRouteIndex(loopPathRoute, logicFps, out _);
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

#if UNITY_EDITOR
    void OnValidate()
    {
        enemyConfigId = enemyConfigId.ToLowerInvariantTrimmed();
        EnemyEncounterConfigValidation.WarnEnemyTypeMismatch(
            this, enemyConfigId, EnemyType.Boss, "MainBossEncounter");
        entryPathRoute?.SyncDurationFromPath();
        loopPathRoute?.SyncDurationFromPath();
    }
#endif
}
