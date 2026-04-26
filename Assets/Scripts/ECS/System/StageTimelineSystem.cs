using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按 <see cref="StageTimelineConfig"/> 在逻辑帧上驱动道中波次、道中 BOSS、关底 BOSS 登场与阶段状态（<see cref="CStageState"/>）。
/// 出怪位置基于 <see cref="GlobalBattleData.AreaData"/>；<see cref="EnemyWaveConfig.movementData"/> 由 <see cref="EnemyMovementBaking"/> 烘焙为 <see cref="CEnemyMovement"/>。
/// 关底/中场 Boss 参数来自独立的 <see cref="MidBossEncounterConfig"/> / <see cref="MainBossEncounterConfig"/> 资产。
/// </summary>
public class StageTimelineSystem : BaseSystem
{
    StageTimelineConfig _config;
    readonly List<EnemyWaveConfig> _sortedWaves = new();
    int _nextWaveIndex;
    bool _waitingForWaveClear;
    readonly List<Entity> _clearWatchEntities = new();

    bool _hasStageAnchor;
    uint _stageStartFrame;

    bool _midBossSpawned;
    bool _mainBossSpawned;
    Entity _midBossEntity;
    Entity _mainBossEntity;
    Entity _stageAuthority;

    uint _bossFightStartElapsed;

    public bool IsActive => _config != null;

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
    {
        EndInternal();
        if (config == null)
        {
            Logger.Warn("[StageTimeline] Begin called with null config.", LogTag.Battle);
            return;
        }

        _config = config;
        _sortedWaves.Clear();
        foreach (var w in config.midStageWaves)
        {
            if (w != null)
                _sortedWaves.Add(w);
        }
        _sortedWaves.Sort((a, b) => a.startFrameOffset.CompareTo(b.startFrameOffset));

        _nextWaveIndex = 0;
        _waitingForWaveClear = false;
        _clearWatchEntities.Clear();
        _hasStageAnchor = false;
        _midBossSpawned = false;
        _mainBossSpawned = false;
        _midBossEntity = Entity.Null;
        _mainBossEntity = Entity.Null;
        _bossFightStartElapsed = 0;

        EnsureStageAuthority();
        ref var st = ref EntityManager.GetComponent<CStageState>(_stageAuthority);
        st.currentState = E_StageState.MidStage;
        st.stateEnterFrame = 0;
        st.currentBossPhaseIndex = -1;
        st.bossEntity = Entity.Null;
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
        _sortedWaves.Clear();
        _nextWaveIndex = 0;
        _waitingForWaveClear = false;
        _clearWatchEntities.Clear();
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
        TrySpawnMidWaves(elapsed, currentFrame);
        TrySpawnMidBoss(elapsed, currentFrame);
        TrySpawnMainBoss(elapsed, currentFrame);
        UpdateBossIntro(elapsed, currentFrame);
        UpdateBossFightPhases(elapsed);
        UpdateBossDefeat();
        UpdateStageTimeout(elapsed, currentFrame);
    }

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
            if (stageElapsed < (uint)wave.startFrameOffset)
                break;

            SpawnWave(wave, _nextWaveIndex, currentFrame);
            _nextWaveIndex++;
        }
    }

    void SpawnWave(EnemyWaveConfig wave, int waveIndexInSorted, uint currentFrame)
    {
        if (string.IsNullOrEmpty(wave.enemyConfigId))
        {
            Logger.Warn("[StageTimeline] Wave skipped: empty enemyConfigId.", LogTag.Battle);
            return;
        }

        var enemyCfg = GameResDB.Instance.GetConfig<EnemyConfig>(wave.enemyConfigId);
        if (enemyCfg == null)
        {
            Logger.Error($"[StageTimeline] EnemyConfig not found: '{wave.enemyConfigId}'", LogTag.Battle);
            return;
        }

        var positions = ComputeSpawnPositions(wave, waveIndexInSorted, currentFrame);
        for (int i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            var e = EntityFactory.CreateEnemy(enemyCfg, p.x, p.y, wave.hpMultiplier);
            if (e.IsNull)
                continue;
            if (EnemyMovementBaking.TryBakeFromWave(wave, currentFrame, p.x, p.y, i, EntityManager, out var motion))
                EntityManager.AddComponent(e, motion);
            EntityManager.AddComponent(e, new CPoolGetTag());
            if (wave.waitForClear)
                _clearWatchEntities.Add(e);
        }

        if (wave.waitForClear && _clearWatchEntities.Count > 0)
            _waitingForWaveClear = true;
    }

    List<Vector2> ComputeSpawnPositions(EnemyWaveConfig wave, int waveIndex, uint currentFrame)
    {
        var area = GlobalBattleData.IsInitialized
            ? GlobalBattleData.AreaData
            : BattleAreaData.Default;

        var list = new List<Vector2>(Mathf.Max(1, wave.count));
        float inset = BattleAreaEdgeInset(area);
        float maxSpanX = Mathf.Max(0.05f, area.Width - 2f * inset);
        float maxSpanY = Mathf.Max(0.05f, area.Height - 2f * inset);
        Vector2 anchor = TopSpawnAnchor(area, wave.spawnOffset);

        switch (wave.spawnPattern)
        {
            case SpawnPattern.BossCenter:
                list.Add(area.Center + wave.spawnOffset);
                ClampSpawnListToBattleArea(area, list);
                return list;

            case SpawnPattern.Line:
            {
                int n = Mathf.Max(1, wave.count);
                float span = Mathf.Min(Mathf.Max(0.01f, wave.spawnAreaSize.x), maxSpanX);
                for (int i = 0; i < n; i++)
                {
                    float t = n == 1 ? 0.5f : i / (float)(n - 1);
                    float x = anchor.x + Mathf.Lerp(-span * 0.5f, span * 0.5f, t);
                    list.Add(new Vector2(x, anchor.y));
                }
                ClampSpawnListToBattleArea(area, list);
                return list;
            }

            case SpawnPattern.Grid:
            {
                int n = Mathf.Max(1, wave.count);
                int cols = Mathf.CeilToInt(Mathf.Sqrt(n));
                int rows = Mathf.CeilToInt(n / (float)cols);
                float sx = Mathf.Min(Mathf.Max(0.01f, wave.spawnAreaSize.x), maxSpanX);
                float sy = Mathf.Min(Mathf.Max(0.01f, wave.spawnAreaSize.y), maxSpanY);
                int k = 0;
                for (int r = 0; r < rows && k < n; r++)
                {
                    for (int c = 0; c < cols && k < n; c++, k++)
                    {
                        float ux = cols == 1 ? 0.5f : c / (float)(cols - 1);
                        float uy = rows == 1 ? 0.5f : r / (float)(rows - 1);
                        float x = anchor.x + Mathf.Lerp(-sx * 0.5f, sx * 0.5f, ux);
                        float y = anchor.y + Mathf.Lerp(-sy * 0.5f, sy * 0.5f, uy);
                        list.Add(new Vector2(x, y));
                    }
                }
                ClampSpawnListToBattleArea(area, list);
                return list;
            }

            case SpawnPattern.Circle:
            {
                int n = Mathf.Max(1, wave.count);
                float minDim = Mathf.Max(0.01f, Mathf.Min(area.Width, area.Height));
                float requested = Mathf.Max(0.01f, wave.spawnAreaSize.x * 0.5f);
                float rad = Mathf.Min(requested, minDim * 0.48f);
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)n;
                    float ang = t * Mathf.PI * 2f;
                    list.Add(new Vector2(anchor.x + Mathf.Cos(ang) * rad, anchor.y + Mathf.Sin(ang) * rad));
                }
                ClampSpawnListToBattleArea(area, list);
                return list;
            }

            case SpawnPattern.Random:
            default:
            {
                float sx = Mathf.Min(Mathf.Max(area.Width * 0.02f, wave.spawnAreaSize.x), maxSpanX);
                float sy = Mathf.Min(Mathf.Max(area.Height * 0.02f, wave.spawnAreaSize.y), maxSpanY);
                int n = Mathf.Max(1, wave.count);
                for (int i = 0; i < n; i++)
                {
                    float rx = Deterministic01(currentFrame, waveIndex, i, 0);
                    float ry = Deterministic01(currentFrame, waveIndex, i, 1);
                    list.Add(new Vector2(
                        anchor.x + (rx - 0.5f) * sx,
                        anchor.y + (ry - 0.5f) * sy));
                }
                ClampSpawnListToBattleArea(area, list);
                return list;
            }
        }
    }

    /// <summary>上沿附近刷怪锚点：内缩量与 <see cref="BattleAreaData.Height"/> 成比例，避免小坐标战斗区仍用固定 40 单位导致刷在区域外。</summary>
    static Vector2 TopSpawnAnchor(BattleAreaData area, Vector2 spawnOffset)
    {
        float topInset = Mathf.Clamp(area.Height * 0.056f, 0.08f, 96f);
        return new Vector2(area.Center.x + spawnOffset.x, area.Top - topInset + spawnOffset.y);
    }

    static float BattleAreaEdgeInset(BattleAreaData area)
    {
        float m = Mathf.Min(area.Width, area.Height);
        return Mathf.Clamp(m * 0.02f, 0.02f, m * 0.45f);
    }

    static Vector2 ClampPointToBattleArea(BattleAreaData area, Vector2 p, float inset)
    {
        float ix = Mathf.Min(inset, area.Width * 0.49f);
        float iy = Mathf.Min(inset, area.Height * 0.49f);
        if (ix * 2f >= area.Width) ix = area.Width * 0.25f;
        if (iy * 2f >= area.Height) iy = area.Height * 0.25f;
        return new Vector2(
            Mathf.Clamp(p.x, area.Left + ix, area.Right - ix),
            Mathf.Clamp(p.y, area.Bottom + iy, area.Top - iy));
    }

    static void ClampSpawnListToBattleArea(BattleAreaData area, List<Vector2> list)
    {
        float inset = BattleAreaEdgeInset(area);
        for (int i = 0; i < list.Count; i++)
            list[i] = ClampPointToBattleArea(area, list[i], inset);
    }

    static float Deterministic01(uint frame, int waveIndex, int spawnIndex, int salt)
    {
        uint x = frame * 2246822519u
                 + (uint)waveIndex * 3266489917u
                 + (uint)spawnIndex * 668265263u
                 + (uint)salt * 374761393u;
        x ^= x >> 16;
        x *= 2654435761u;
        x ^= x >> 13;
        x *= 3266489917u;
        x ^= x >> 16;
        return (x & 0xffffffu) / (float)0xffffffu;
    }

    void TrySpawnMidBoss(uint stageElapsed, uint currentFrame)
    {
        if (!MidBossConfigured || _midBossSpawned)
            return;

        var encounter = _config.midBossEncounter;
        if (stageElapsed < (uint)encounter.spawnFrameOffset)
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
        _midBossEntity = EntityFactory.CreateEnemy(cfg, pos.x, pos.y, 1f);
        if (!_midBossEntity.IsNull)
            EntityManager.AddComponent(_midBossEntity, new CPoolGetTag());
        _midBossSpawned = true;
    }

    void TrySpawnMainBoss(uint stageElapsed, uint currentFrame)
    {
        if (!MainBossConfigured || _mainBossSpawned)
            return;

        var encounter = _config.mainBossEncounter;
        if (stageElapsed < (uint)encounter.spawnFrameOffset)
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
            EntityManager.AddComponent(_mainBossEntity, new CPoolGetTag());

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
        uint introEnd = (uint)encounter.spawnFrameOffset + (uint)encounter.bossIntroDurationFrames;
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
