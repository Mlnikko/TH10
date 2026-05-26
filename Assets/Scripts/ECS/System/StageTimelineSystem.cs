using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>编辑器/工具用：限定 <see cref="StageTimelineSystem"/> 仅驱动部分时间轴内容。</summary>
public enum E_StageTimelinePreviewScope
{
    FullTimeline = 0,
    SingleMidStageWave,
    MidBossEncounter,
    MainBossEncounter,
}

/// <summary>
/// 按 <see cref="StageTimelineConfig"/> 在逻辑帧上驱动道中波次、道中 BOSS、关底 BOSS 登场与阶段状态（<see cref="CStageState"/>）。
/// 出怪位置基于 <see cref="GlobalBattleData.AreaData"/>；<see cref="EnemyWaveConfig.pathRoute"/> 烘焙为 <see cref="CEnemyPathMovement"/>。
/// 关底/中场 Boss 参数来自独立的 <see cref="MidBossEncounterConfig"/> / <see cref="MainBossEncounterConfig"/> 资产。
/// </summary>
public class StageTimelineSystem : BaseSystem
{
    StageTimelineConfig _config;
    readonly List<EnemyWaveConfig> _sortedWaves = new();
    int _nextWaveIndex;
    bool _waitingForWaveClear;
    readonly List<Entity> _clearWatchEntities = new();
    readonly List<PendingSequentialSpawn> _pendingSequentialSpawns = new();

    bool _hasStageAnchor;
    uint _stageStartFrame;

    bool _midBossSpawned;
    bool _mainBossSpawned;
    Entity _midBossEntity;
    Entity _mainBossEntity;
    Entity _stageAuthority;

    uint _bossFightStartElapsed;

    E_StageTimelinePreviewScope _previewScope = E_StageTimelinePreviewScope.FullTimeline;
    int _previewMidStageWaveIndex;

    public bool IsActive => _config != null;
    public E_StageTimelinePreviewScope PreviewScope => _previewScope;

    /// <summary>
    /// 读取关卡权威实体上的 <see cref="CStageState"/>（时间线未开始时返回 false）。
    /// </summary>
    public bool TryGetStageState(out E_StageState state)
    {
        state = E_StageState.None;
        if (!EntityManager.IsValid(_stageAuthority))
            return false;
        state = EntityManager.GetComponent<CStageState>(_stageAuthority).currentState;
        return true;
    }

    bool MidBossConfigured =>
        _config != null
        && _config.midBossEncounter != null
        && _config.midBossEncounter.enabled
        && !string.IsNullOrEmpty(_config.midBossEncounter.enemyConfigId);

    bool MainBossConfigured =>
        _config != null
        && _config.mainBossEncounter != null
        && _config.mainBossEncounter.enabled
        && !string.IsNullOrEmpty(_config.mainBossEncounter.enemyConfigId);

    /// <summary>
    /// 开始本关时间线（通常在战斗 World 就绪且逻辑帧计时器已对齐到起始帧之后调用）。
    /// </summary>
    public void Begin(StageTimelineConfig config)
        => Begin(config, E_StageTimelinePreviewScope.FullTimeline, 0);

    /// <summary>
    /// 开始时间线；<paramref name="previewScope"/> 非 <see cref="E_StageTimelinePreviewScope.FullTimeline"/> 时仅驱动对应片段（编辑器预览）。
    /// </summary>
    public void Begin(StageTimelineConfig config, E_StageTimelinePreviewScope previewScope, int midStageWaveIndex = 0)
    {
        EndInternal();
        if (config == null)
        {
            Logger.Warn("[StageTimeline] Begin called with null config.", LogTag.Battle);
            return;
        }

        _previewScope = previewScope;
        _previewMidStageWaveIndex = midStageWaveIndex;
        _config = config;
        _sortedWaves.Clear();

        if (previewScope == E_StageTimelinePreviewScope.SingleMidStageWave)
        {
            if (config.midStageWaves == null
                || midStageWaveIndex < 0
                || midStageWaveIndex >= config.midStageWaves.Count
                || config.midStageWaves[midStageWaveIndex] == null)
            {
                Logger.Warn($"[StageTimeline] Invalid preview wave index {midStageWaveIndex}.", LogTag.Battle);
                EndInternal();
                return;
            }

            _sortedWaves.Add(config.midStageWaves[midStageWaveIndex]);
        }
        else if (previewScope == E_StageTimelinePreviewScope.FullTimeline)
        {
            foreach (var w in config.midStageWaves)
            {
                if (w != null)
                    _sortedWaves.Add(w);
            }
            _sortedWaves.Sort((a, b) => a.startFrameOffset.CompareTo(b.startFrameOffset));
        }

        _nextWaveIndex = 0;
        _waitingForWaveClear = false;
        _clearWatchEntities.Clear();
        _pendingSequentialSpawns.Clear();
        EnemyPathBakeCache.Clear();
        _hasStageAnchor = false;
        _midBossSpawned = false;
        _mainBossSpawned = false;
        _midBossEntity = Entity.Null;
        _mainBossEntity = Entity.Null;
        _bossFightStartElapsed = 0;

        RebuildWavePathBakes();
        EnsureStageAuthority();
        ref var st = ref EntityManager.GetComponent<CStageState>(_stageAuthority);
        st.currentState = E_StageState.MidStage;
        st.stateEnterFrame = 0;
        st.currentBossPhaseIndex = -1;
        st.bossEntity = Entity.Null;
    }

    void RebuildWavePathBakes()
    {
        EnemyPathBakeCache.Clear();
        uint fps = GameManager.logicFPS > 0 ? (uint)GameManager.logicFPS : 60;
        for (int i = 0; i < _sortedWaves.Count; i++)
        {
            var wave = _sortedWaves[i];
            if (wave == null)
                continue;
            wave.BakeLogicTiming(fps);
            wave.BakePathRouteIfNeeded(fps);
        }

        _config.midBossEncounter?.BakeLogicTiming(fps);
        _config.midBossEncounter?.BakePathRoutesIfNeeded(fps);
        _config.mainBossEncounter?.BakeLogicTiming(fps);
        _config.mainBossEncounter?.BakePathRoutesIfNeeded(fps);
    }

    /// <summary>
    /// 结束本关时间线（例如切关或重开战斗前调用）。
    /// </summary>
    public void End()
    {
        EndInternal();
    }

    void EndInternal()
    {
        _config = null;
        _previewScope = E_StageTimelinePreviewScope.FullTimeline;
        _previewMidStageWaveIndex = 0;
        _sortedWaves.Clear();
        _nextWaveIndex = 0;
        _waitingForWaveClear = false;
        _clearWatchEntities.Clear();
        _pendingSequentialSpawns.Clear();
        EnemyPathBakeCache.Clear();
        _hasStageAnchor = false;
        _midBossSpawned = false;
        _mainBossSpawned = false;
        _midBossEntity = Entity.Null;
        _mainBossEntity = Entity.Null;
        if (EntityManager.IsValid(_stageAuthority))
            EntityManager.DestroyEntity(_stageAuthority);
        _stageAuthority = Entity.Null;
    }

    void EnsureStageAuthority()
    {
        if (EntityManager.IsValid(_stageAuthority))
            return;
        _stageAuthority = EntityManager.CreateEntity();
        EntityManager.AddComponent(_stageAuthority, new CStageState
        {
            currentState = E_StageState.None,
            stateEnterFrame = 0,
            currentBossPhaseIndex = -1,
            bossEntity = Entity.Null
        });
    }

    public override void OnLogicTick(uint currentFrame)
    {
        if (_config == null)
            return;

        if (!_hasStageAnchor)
        {
            _stageStartFrame = currentFrame;
            _hasStageAnchor = true;
        }

        uint elapsed = currentFrame - _stageStartFrame;

        UpdateClearWatch();
        ProcessPendingSequentialSpawns(currentFrame);

        if (_previewScope == E_StageTimelinePreviewScope.FullTimeline
            || _previewScope == E_StageTimelinePreviewScope.SingleMidStageWave)
            TrySpawnMidWaves(elapsed, currentFrame);

        if (_previewScope == E_StageTimelinePreviewScope.FullTimeline
            || _previewScope == E_StageTimelinePreviewScope.MidBossEncounter)
            TrySpawnMidBoss(elapsed, currentFrame);

        if (_previewScope == E_StageTimelinePreviewScope.FullTimeline
            || _previewScope == E_StageTimelinePreviewScope.MainBossEncounter)
        {
            TrySpawnMainBoss(elapsed, currentFrame);
            UpdateBossIntro(elapsed, currentFrame);
            UpdateBossFightPhases(elapsed);
            UpdateBossDefeat();
        }

        if (_previewScope == E_StageTimelinePreviewScope.FullTimeline)
            UpdateStageTimeout(elapsed, currentFrame);
    }

    static bool IsScopedPreview(E_StageTimelinePreviewScope scope) =>
        scope != E_StageTimelinePreviewScope.FullTimeline;

    void UpdateClearWatch()
    {
        if (!_waitingForWaveClear)
            return;

        for (int i = _clearWatchEntities.Count - 1; i >= 0; i--)
        {
            if (!EntityManager.IsValid(_clearWatchEntities[i]))
                _clearWatchEntities.RemoveAt(i);
        }

        if (_clearWatchEntities.Count == 0)
            _waitingForWaveClear = false;
    }

    void TrySpawnMidWaves(uint stageElapsed, uint currentFrame)
    {
        ref var st = ref EntityManager.GetComponent<CStageState>(_stageAuthority);
        if (st.currentState != E_StageState.MidStage)
            return;

        while (_nextWaveIndex < _sortedWaves.Count && !_waitingForWaveClear)
        {
            var wave = _sortedWaves[_nextWaveIndex];
            if (!IsScopedPreview(_previewScope) && stageElapsed < (uint)wave.startFrameOffset)
                break;

            int spawnIndex = _previewScope == E_StageTimelinePreviewScope.SingleMidStageWave
                ? _previewMidStageWaveIndex
                : _nextWaveIndex;
            SpawnWave(wave, spawnIndex, currentFrame);
            _nextWaveIndex++;
        }
    }

    void SpawnWave(EnemyWaveConfig wave, int waveIndexInSorted, uint currentFrame)
    {
        wave.EnsureSpawnQueueMigrated();
        if (wave.spawnQueue == null || wave.spawnQueue.Length == 0)
        {
            Logger.Warn("[StageTimeline] Wave skipped: empty spawn queue.", LogTag.Battle);
            return;
        }

        var area = GlobalBattleData.IsInitialized
            ? GlobalBattleData.AreaData
            : BattleAreaData.Default;
        var positions = EnemyWaveSpawnMath.ComputeSpawnPositions(wave, area, waveIndexInSorted, currentFrame);
        int spawnCount = wave.ResolveSpawnCount();
        if (spawnCount <= 0 || positions.Count == 0)
            return;

        if (wave.UsesSequentialSpawn)
        {
            if (!TrySpawnWaveEntry(wave, waveIndexInSorted, currentFrame, positions, 0))
                return;

            if (spawnCount > 1)
            {
                _pendingSequentialSpawns.Add(new PendingSequentialSpawn
                {
                    wave = wave,
                    waveIndexInSorted = waveIndexInSorted,
                    positions = positions,
                    nextEntryIndex = 1,
                    nextSpawnFrame = currentFrame + ResolveDelayFrames(wave, 1)
                });
            }
        }
        else
        {
            for (int i = 0; i < spawnCount; i++)
                TrySpawnWaveEntry(wave, waveIndexInSorted, currentFrame, positions, i);
        }

        if (wave.waitForClear && _clearWatchEntities.Count > 0)
            _waitingForWaveClear = true;
    }

    void ProcessPendingSequentialSpawns(uint currentFrame)
    {
        for (int j = _pendingSequentialSpawns.Count - 1; j >= 0; j--)
        {
            var job = _pendingSequentialSpawns[j];
            if (currentFrame < job.nextSpawnFrame)
                continue;

            if (!TrySpawnWaveEntry(job.wave, job.waveIndexInSorted, currentFrame, job.positions, job.nextEntryIndex))
            {
                _pendingSequentialSpawns.RemoveAt(j);
                continue;
            }

            job.nextEntryIndex++;
            int total = job.wave.ResolveSpawnCount();
            if (job.nextEntryIndex >= total)
            {
                _pendingSequentialSpawns.RemoveAt(j);
                continue;
            }

            job.nextSpawnFrame = currentFrame + ResolveDelayFrames(job.wave, job.nextEntryIndex);
            _pendingSequentialSpawns[j] = job;
        }
    }

    static uint ResolveDelayFrames(EnemyWaveConfig wave, int entryIndex)
    {
        if (wave.spawnQueue != null && entryIndex < wave.spawnQueue.Length)
        {
            float sec = wave.spawnQueue[entryIndex].delayAfterPreviousSeconds;
            if (sec > 0f)
            {
                uint fps = GameManager.logicFPS > 0 ? (uint)GameManager.logicFPS : 60;
                return (uint)Mathf.Max(1, Mathf.RoundToInt(sec * fps));
            }
        }

        return wave.spawnIntervalFrames > 0
            ? (uint)wave.spawnIntervalFrames
            : 1u;
    }

    bool TrySpawnWaveEntry(
        EnemyWaveConfig wave,
        int waveIndexInSorted,
        uint currentFrame,
        List<Vector2> positions,
        int entryIndex)
    {
        if (!TryResolveSpawnEntry(wave, entryIndex, positions, out EnemyConfig enemyCfg, out Vector2 pos))
            return false;

        var e = EntityFactory.CreateEnemy(enemyCfg, pos.x, pos.y, wave.hpMultiplier);
        if (e.IsNull)
            return false;

        EnemyMovementBaking.TryAttachMovementFromWave(EntityManager, e, wave, entryIndex, currentFrame, pos.x, pos.y);

        if (wave.waveDropMode != E_WaveDropOverrideMode.UseEnemyConfig)
        {
            EntityManager.AddComponent(e, new CEnemyDeathLoot
            {
                waveDropMode = wave.waveDropMode,
                waveDrops = wave.waveDropOnDeathBaked ?? Array.Empty<BakedDeathDropEntry>()
            });
        }

        EntityManager.AddComponent(e, new CPoolGetTag());
        if (wave.waitForClear)
            _clearWatchEntities.Add(e);
        return true;
    }

    static bool TryResolveSpawnEntry(
        EnemyWaveConfig wave,
        int entryIndex,
        List<Vector2> positions,
        out EnemyConfig enemyCfg,
        out Vector2 pos)
    {
        enemyCfg = null;
        pos = default;

        wave.EnsureSpawnQueueMigrated();
        if (wave.spawnQueue == null || entryIndex < 0 || entryIndex >= wave.spawnQueue.Length)
            return false;

        var entry = wave.spawnQueue[entryIndex];
        string enemyId = entry.enemyConfigId;
        int slot = entry.spawnSlotIndex >= 0 ? entry.spawnSlotIndex : entryIndex;

        if (string.IsNullOrWhiteSpace(enemyId))
            return false;

        enemyCfg = GameResDB.Instance.GetConfig<EnemyConfig>(enemyId);
        if (enemyCfg == null)
        {
            Logger.Error($"[StageTimeline] EnemyConfig not found: '{enemyId}'", LogTag.Battle);
            return false;
        }

        if (positions == null || positions.Count == 0)
            return false;
        int posIndex = Mathf.Clamp(slot, 0, positions.Count - 1);
        pos = positions[posIndex];
        return true;
    }

    struct PendingSequentialSpawn
    {
        public EnemyWaveConfig wave;
        public int waveIndexInSorted;
        public List<Vector2> positions;
        public int nextEntryIndex;
        public uint nextSpawnFrame;
    }

    void TrySpawnMidBoss(uint stageElapsed, uint currentFrame)
    {
        if (!MidBossConfigured || _midBossSpawned)
            return;

        var encounter = _config.midBossEncounter;
        if (!IsScopedPreview(_previewScope) && stageElapsed < (uint)encounter.spawnFrameOffset)
            return;

        var cfg = GameResDB.Instance.GetConfig<EnemyConfig>(encounter.enemyConfigId);
        if (cfg == null)
        {
            Logger.Error($"[StageTimeline] Mid boss EnemyConfig not found: '{encounter.enemyConfigId}'", LogTag.Battle);
            _midBossSpawned = true;
            return;
        }

        var area = GlobalBattleData.IsInitialized ? GlobalBattleData.AreaData : BattleAreaData.Default;
        Vector2 pos = area.Center + encounter.spawnOffset + new Vector2(0f, area.Height * encounter.yHeightNorm);
        float hpMult = encounter.ResolveHpMultiplier(cfg);
        _midBossEntity = EntityFactory.CreateEnemy(cfg, pos.x, pos.y, hpMult);
        if (!_midBossEntity.IsNull)
        {
            MidBossEncounterSpawn.ApplyToEntity(
                EntityManager, _midBossEntity, encounter, cfg, currentFrame, pos.x, pos.y);

            EntityManager.AddComponent(_midBossEntity, new CNoOffscreenRecycleTag());
            EntityManager.AddComponent(_midBossEntity, new CPoolGetTag());
        }
        _midBossSpawned = true;
    }

    void TrySpawnMainBoss(uint stageElapsed, uint currentFrame)
    {
        if (!MainBossConfigured || _mainBossSpawned)
            return;

        var encounter = _config.mainBossEncounter;
        if (!IsScopedPreview(_previewScope) && stageElapsed < (uint)encounter.spawnFrameOffset)
            return;

        var cfg = GameResDB.Instance.GetConfig<EnemyConfig>(encounter.enemyConfigId);
        if (cfg == null)
        {
            Logger.Error($"[StageTimeline] Main boss EnemyConfig not found: '{encounter.enemyConfigId}'", LogTag.Battle);
            _mainBossSpawned = true;
            return;
        }

        var area = GlobalBattleData.IsInitialized ? GlobalBattleData.AreaData : BattleAreaData.Default;
        Vector2 pos = area.Center + encounter.spawnOffset + new Vector2(0f, area.Height * encounter.yHeightNorm);
        _mainBossEntity = EntityFactory.CreateEnemy(cfg, pos.x, pos.y, 1f);
        if (!_mainBossEntity.IsNull)
        {
            EntityManager.AddComponent(_mainBossEntity, new CNoOffscreenRecycleTag());
            EntityManager.AddComponent(_mainBossEntity, new CPoolGetTag());
        }

        _mainBossSpawned = true;

        ref var st = ref EntityManager.GetComponent<CStageState>(_stageAuthority);
        st.currentState = E_StageState.BossIntro;
        st.stateEnterFrame = currentFrame;
        st.bossEntity = _mainBossEntity;
        st.currentBossPhaseIndex = -1;
    }

    void UpdateBossIntro(uint stageElapsed, uint currentFrame)
    {
        if (!MainBossConfigured)
            return;

        ref var st = ref EntityManager.GetComponent<CStageState>(_stageAuthority);
        if (st.currentState != E_StageState.BossIntro)
            return;

        var encounter = _config.mainBossEncounter;
        uint introEnd = IsScopedPreview(_previewScope)
            ? (uint)encounter.bossIntroDurationFrames
            : (uint)encounter.spawnFrameOffset + (uint)encounter.bossIntroDurationFrames;
        if (stageElapsed < introEnd)
            return;

        st.currentState = E_StageState.BossFight;
        st.stateEnterFrame = currentFrame;
        st.bossEntity = _mainBossEntity;
        _bossFightStartElapsed = stageElapsed;
    }

    void UpdateBossFightPhases(uint stageElapsed)
    {
        if (!MainBossConfigured)
            return;

        ref var st = ref EntityManager.GetComponent<CStageState>(_stageAuthority);
        if (st.currentState != E_StageState.BossFight)
            return;

        var phases = _config.mainBossEncounter.bossPhases;
        if (phases == null || phases.Count == 0)
            return;

        uint fightElapsed = stageElapsed - _bossFightStartElapsed;
        int best = -1;
        for (int i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            if (phase == null)
                continue;
            if (phase.triggerType != BossPhaseConfig.TriggerType.Time)
                continue;
            if (fightElapsed >= (uint)phase.triggerFrameOffset)
                best = i;
        }
        if (best >= 0)
            st.currentBossPhaseIndex = best;
    }

    void UpdateBossDefeat()
    {
        if (!MainBossConfigured)
            return;

        ref var st = ref EntityManager.GetComponent<CStageState>(_stageAuthority);
        if (st.currentState != E_StageState.BossFight && st.currentState != E_StageState.BossIntro)
            return;

        if (EntityManager.IsValid(_mainBossEntity))
        {
            ref var hp = ref EntityManager.GetComponent<CEnemy>(_mainBossEntity);
            if (hp.currentHealth > 0)
                return;
        }

        st.currentState = E_StageState.BossDefeated;
        st.bossEntity = Entity.Null;
    }

    void UpdateStageTimeout(uint stageElapsed, uint currentFrame)
    {
        int maxFrames = _config.maxStageLogicFrames;
        if (maxFrames <= 0)
            return;
        if (stageElapsed < (uint)maxFrames)
            return;

        ref var st = ref EntityManager.GetComponent<CStageState>(_stageAuthority);
        if (st.currentState == E_StageState.StageClear)
            return;

        st.currentState = E_StageState.StageClear;
        st.stateEnterFrame = currentFrame;
    }
}
