using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public struct PlayerBattleData
{
    public byte playerIndex;
    public E_Character characterId;
    public E_Weapon weaponId;

    public PlayerBattleData(byte index, E_Character character, E_Weapon weapon)
    {
        playerIndex = index;
        characterId = character;
        weaponId = weapon;
    }
}

public enum E_BattleStatus
{
    Prepare,
    InBattle
}

/// <summary>战斗 HUD 只读快照（供 UI 轮询）。</summary>
public readonly struct BattleHudSnapshot
{
    public readonly int Score;
    public readonly int HiScore;
    public readonly int HealthCurrent;
    public readonly int HealthMax;
    public readonly int PowerOrbs;

    public BattleHudSnapshot(int score, int hiScore, int healthCurrent, int healthMax, int powerOrbs)
    {
        Score = score;
        HiScore = hiScore;
        HealthCurrent = healthCurrent;
        HealthMax = healthMax;
        PowerOrbs = powerOrbs;
    }
}

public static class GlobalBattleData
{
    public static BattleAreaData AreaData { get; private set; }
    public static PlayerSpawnData SpawnData { get; private set; }
    public static DropItemCollectData DropItemCollectData { get; private set; }

    public static bool IsInitialized { get; private set; }

    /// <summary>当前战斗会话得分（切关/重开战斗时由 <see cref="BattleManager.BeginBattleSession"/> 重置）。</summary>
    public static int SessionScore { get; private set; }

    public static void Initialize(BattleAreaConfig config)
    {
        AreaData = config.battleAreaData;
        SpawnData = config.playerSpawnData;
        DropItemCollectData = config.dropItemCollectData;
        IsInitialized = true;
    }

    public static void ResetBattleSessionStats()
    {
        SessionScore = 0;
    }

    public static void AddSessionScore(int delta)
    {
        if (delta <= 0) return;
        SessionScore += delta;
    }

#if UNITY_EDITOR
    /// <summary>编辑器 ConfigViewer 预览结束后还原战斗区全局状态。</summary>
    public static void ResetForEditorPreview()
    {
        AreaData = default;
        SpawnData = default;
        DropItemCollectData = default;
        IsInitialized = false;
    }
#endif
}

/// <summary>
/// 战斗会话入口：进场景 + 准备 UI + ECS 开战均由此类编排。
/// </summary>
public class BattleManager : SingletonMono<BattleManager>
{
    public const string BattleSceneName = "BattleScene";

    public bool isSinglePlayerMode;

    public E_BattleStatus CurrentStatus { get; private set; } = E_BattleStatus.Prepare;
    public List<PlayerBattleData> allPlayerDatas = new(4);

    readonly bool[] _activePlayers = new bool[4];

    int TotalPlayers => allPlayerDatas.Count;

    World _battleWorld;

    public World ActiveBattleWorld => _battleWorld;

    readonly BattlePrepareCharacterRegistry _prepareCharacterRegistry = new();

    /// <summary>某玩家确认准备并锁定角色后（playerIndex, characterId）。</summary>
    public event Action<byte, E_Character> OnPrepareCharacterLocked;

    /// <summary>某玩家取消准备并释放角色锁定。</summary>
    public event Action<byte> OnPrepareCharacterReleased;

    [Header("关卡时间线")]
    [Tooltip("StageTimelineConfig 的 ConfigId（SO 文件名小写）；运行时从 GameResDB 读取")]
    [SerializeField] string _activeStageTimelineConfigId = "stagetimeline_1";

    public string ActiveStageTimelineConfigId => _activeStageTimelineConfigId;

    #region 进战斗场景（加载 + 准备面板）

    /// <summary>
    /// 统一入口：关闭 UI → 加载 BattleScene → 重置准备态 → 同步战斗区 → 打开准备面板。
    /// 单机菜单与联机 <see cref="RoomManager.HandleEnterBattleScene"/> 均调用此方法。
    /// </summary>
    public async Task<bool> LoadBattleSceneAndShowPrepareAsync()
    {
        UIManager.Instance.CloseAll();

        if (!await SceneLoader.LoadSceneAsync(BattleSceneName))
        {
            Logger.Error("[Battle] Failed to load BattleScene.", LogTag.Battle);
            return false;
        }

        return await ShowBattlePrepareAsync();
    }

    /// <summary>已在 BattleScene 时仅打开准备面板（一般请用 <see cref="LoadBattleSceneAndShowPrepareAsync"/>）。</summary>
    public async Task<bool> ShowBattlePrepareAsync()
    {
        ResetPrepareSession();
        EnsureGlobalBattleDataReady();

        if (!GlobalBattleData.IsInitialized)
        {
            Logger.Critical("[Battle] GlobalBattleData not ready; check GameResourceManifest.battleAreaConfigId.", LogTag.Battle);
            return false;
        }

        await UIManager.Instance.ShowPanelAsync<BattlePreparePanel>();
        return true;
    }

    /// <summary>新开一局准备阶段：清空玩家列表、释放旧 ECS 世界。</summary>
    void ResetPrepareSession()
    {
        allPlayerDatas.Clear();
        Array.Clear(_activePlayers, 0, _activePlayers.Length);
        _prepareCharacterRegistry.Reset();
        CurrentStatus = E_BattleStatus.Prepare;
        DisposeBattleWorld();
    }

    public bool IsMultiplayerPrepare =>
        NetworkManager.Instance != null
        && NetworkManager.Instance.NetworkRole != NetworkRole.None;

    public bool IsPrepareCharacterAvailable(E_Character character, byte forPlayerIndex) =>
        !IsMultiplayerPrepare || _prepareCharacterRegistry.IsAvailable(character, forPlayerIndex);

    public bool IsPlayerPrepareCharacterLocked(byte playerIndex) =>
        _prepareCharacterRegistry.GetPick(playerIndex) != E_Character.None;

    public bool TryGetPrepareCharacterLocker(E_Character character, out byte playerIndex) =>
        _prepareCharacterRegistry.TryGetLocker(character, out playerIndex);

    /// <summary>确认准备时锁定角色；失败表示已被其他已准备玩家占用。</summary>
    public bool TryLockPrepareCharacter(byte playerIndex, E_Character character) =>
        character != E_Character.None && _prepareCharacterRegistry.TryClaim(playerIndex, character);

    /// <summary>房主本地确认准备：锁定、写入数据并广播。</summary>
    public bool HostSubmitPrepareReady(PlayerBattleData data)
    {
        if (!TryValidateAndLockPrepareReady(data))
            return false;

        SetOrUpdatePlayerData(data);
        NetworkManager.Instance.Broadcast(new BattleReadyMSG { playerBattleData = data });
        return true;
    }

    /// <summary>房主收到客户端的准备消息。</summary>
    public bool HostReceiveClientPrepareReady(PlayerBattleData data)
    {
        if (!TryValidateAndLockPrepareReady(data))
            return false;

        SetOrUpdatePlayerData(data);
        NetworkManager.Instance.Broadcast(new BattleReadyMSG { playerBattleData = data });
        Logger.Info($"[BattlePrepare] Player {data.playerIndex} ready: {data.characterId} / {data.weaponId}", LogTag.Battle);
        return true;
    }

    /// <summary>客户端收到房主广播的准备消息（含己方）。</summary>
    public void ClientApplyPrepareReadyBroadcast(PlayerBattleData data)
    {
        if (data.characterId != E_Character.None)
            _prepareCharacterRegistry.TryClaim(data.playerIndex, data.characterId);
        SetOrUpdatePlayerData(data);
    }

    public bool HostSubmitPrepareCancel(byte playerIndex)
    {
        RemovePreparePlayerData(playerIndex);
        NetworkManager.Instance.Broadcast(new BattlePrepareCancelMSG { playerIndex = playerIndex });
        return true;
    }

    public bool HostReceiveClientPrepareCancel(byte playerIndex)
    {
        RemovePreparePlayerData(playerIndex);
        NetworkManager.Instance.Broadcast(new BattlePrepareCancelMSG { playerIndex = playerIndex });
        return true;
    }

    public void ClientApplyPrepareCancelBroadcast(byte playerIndex)
    {
        RemovePreparePlayerData(playerIndex);
    }

    public void RemovePreparePlayerData(byte playerIndex)
    {
        _prepareCharacterRegistry.Release(playerIndex);
        for (int i = allPlayerDatas.Count - 1; i >= 0; i--)
        {
            if (allPlayerDatas[i].playerIndex == playerIndex)
                allPlayerDatas.RemoveAt(i);
        }
        OnPrepareCharacterReleased?.Invoke(playerIndex);
    }

    bool TryValidateAndLockPrepareReady(PlayerBattleData data)
    {
        if (data.characterId == E_Character.None || data.weaponId == E_Weapon.None)
        {
            Logger.Warn($"[BattlePrepare] Invalid ready data from player {data.playerIndex}.", LogTag.Battle);
            return false;
        }

        if (!IsMultiplayerPrepare)
            return true;

        if (TryLockPrepareCharacter(data.playerIndex, data.characterId))
            return true;

        Logger.Warn(
            $"[BattlePrepare] Character {data.characterId} already locked by another ready player.",
            LogTag.Battle);
        return false;
    }

    static bool ValidateUniqueCharacterPicks(IReadOnlyList<PlayerBattleData> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            E_Character a = players[i].characterId;
            if (a == E_Character.None)
                return false;
            for (int j = i + 1; j < players.Count; j++)
            {
                if (players[j].characterId == a)
                    return false;
            }
        }
        return true;
    }

    void EnsureGlobalBattleDataReady()
    {
        if (GlobalBattleData.IsInitialized || !GameResDB.IsInitialized)
            return;

        var manifest = ResManager.Instance?.Manifest;
        if (manifest == null || string.IsNullOrEmpty(manifest.battleAreaConfigId))
            return;

        var battleArea = GameResDB.Instance.GetConfig<BattleAreaConfig>(manifest.battleAreaConfigId);
        if (battleArea != null)
            GlobalBattleData.Initialize(battleArea);
    }

    #endregion

    #region 开战（ECS Bootstrap）

    public void StartSinglePlayerBattle()
    {
        BeginBattleSession(singlePlayer: true, logicStartFrame: 0, remotePlayerDatas: null);
    }

    public void StartMutiPlayerBattleForHost()
    {
        var playerDatas = allPlayerDatas.ToArray();
        uint startFrame = 0;
        uint randomSeed = 0;

        NetworkManager.Instance.Broadcast(new BattleStartMSG
        {
            startFrame = startFrame,
            randomSeed = randomSeed,
            playerDatas = playerDatas
        });

        BeginBattleSession(singlePlayer: false, startFrame, playerDatas);
    }

    public void StartMutiPlayerBattleForClient(uint startFrame, uint randomSeed, PlayerBattleData[] playerDatas)
    {
        BeginBattleSession(singlePlayer: false, startFrame, playerDatas);
    }

    /// <summary>单机 / 联机开战的唯一入口（在 Bootstrap 之前整理玩家与输入状态）。</summary>
    void BeginBattleSession(bool singlePlayer, uint logicStartFrame, PlayerBattleData[] remotePlayerDatas)
    {
        isSinglePlayerMode = singlePlayer;

        if (remotePlayerDatas != null)
        {
            allPlayerDatas.Clear();
            for (int i = 0; i < remotePlayerDatas.Length; i++)
                AddPlayerData(remotePlayerDatas[i]);
        }

        if (allPlayerDatas.Count == 0)
        {
            Logger.Error("[Battle] No player data; cannot start session.", LogTag.Battle);
            return;
        }

        if (!singlePlayer && !ValidateUniqueCharacterPicks(allPlayerDatas))
        {
            Logger.Error("[Battle] Duplicate character picks among players; cannot start session.", LogTag.Battle);
            return;
        }

        EnsureGlobalBattleDataReady();
        if (!GlobalBattleData.IsInitialized)
        {
            Logger.Critical("[Battle] GlobalBattleData not initialized; aborting session start.", LogTag.Battle);
            return;
        }

        RebuildActivePlayerMask();
        InputManager.Instance?.ClearAllInputs();
        BootstrapBattleSession(logicStartFrame);
    }

    void RebuildActivePlayerMask()
    {
        Array.Clear(_activePlayers, 0, _activePlayers.Length);
        for (int i = 0; i < allPlayerDatas.Count; i++)
            _activePlayers[allPlayerDatas[i].playerIndex] = true;
    }

    /// <summary>
    /// ECS 世界创建顺序：释放旧世界 → 战斗区+池 → 新世界 → 逻辑帧 → 时间轴 → 玩家 → InBattle → HUD。
    /// </summary>
    void BootstrapBattleSession(uint logicStartFrame)
    {
        CurrentStatus = E_BattleStatus.Prepare;
        GlobalBattleData.ResetBattleSessionStats();
        DisposeBattleWorld();
        PrepareBattleInfrastructure();
        CreateBattleWorld();
        _battleWorld.LogicFrameTimer.ResetToFrame(logicStartFrame);
        TryBeginStageTimeline();
        GeneratePlayer();
        CurrentStatus = E_BattleStatus.InBattle;
        ShowBattleUIPanelFireAndForget();
    }

    #endregion

    public void SetActiveStageTimelineConfigId(string configId)
    {
        if (string.IsNullOrEmpty(configId)) return;
        _activeStageTimelineConfigId = configId.ToLowerInvariantTrimmed();
    }

    StageTimelineConfig ResolveStageTimelineForBattle()
    {
        if (!GameResDB.IsInitialized)
            return null;
        return GameResDB.Instance.GetConfig<StageTimelineConfig>(_activeStageTimelineConfigId);
    }

    public bool TryGetStageState(out E_StageState state)
    {
        state = E_StageState.None;
        if (_battleWorld == null) return false;
        var timeline = _battleWorld.GetSystem<StageTimelineSystem>();
        return timeline != null && timeline.TryGetStageState(out state);
    }

    void CreateBattleWorld()
    {
        _battleWorld = new World();
        _battleWorld.AddSystem<StageTimelineSystem>();
        _battleWorld.AddSystem<EnemyMovementSystem>();
        _battleWorld.AddSystem<DropItemSystem>();
        _battleWorld.AddSystem<CollisionSystem>();
        _battleWorld.AddSystem<CollisionLogicSystem>();
        _battleWorld.AddSystem<PlayerControlSystem>();
        _battleWorld.AddSystem<DropItemCollectSystem>();
        _battleWorld.AddSystem<DropItemMagnetSystem>();
        _battleWorld.AddSystem<DanmakuSystem>();
        _battleWorld.AddSystem<DanmakuEmitSystem>();
        _battleWorld.AddSystem<PresentationSystem>();
        _battleWorld.AddSystem<PresentationPoseSystem>();
        Logger.Info("Battle ECS World initialized.");
    }

    void DisposeBattleWorld()
    {
        if (_battleWorld == null) return;
        _battleWorld.GetSystem<StageTimelineSystem>()?.End();
        _battleWorld.Dispose();
        _battleWorld = null;
    }

    const string PrefabIdBattlePanel = "battlepanel";

    void ShowBattleUIPanelFireAndForget()
    {
        _ = ShowBattleUIPanelAsync();
    }

    async Task ShowBattleUIPanelAsync()
    {
        try
        {
            if (UIManager.Instance == null)
            {
                Logger.Error("[Battle] UIManager.Instance 为空，无法打开 BattleUIPanel。", LogTag.UI);
                return;
            }

            var panel = await UIManager.Instance.ShowPanelAsync<BattleUIPanel>();
            if (panel == null)
                Logger.Error($"[Battle] BattleUIPanel 未创建成功，请检查 prefab_{PrefabIdBattlePanel} 与 UIManager 日志。", LogTag.UI);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Battle] Failed to open BattleUIPanel (prefab_{PrefabIdBattlePanel}): {ex.Message}", LogTag.UI);
        }
    }

    public bool TryGetBattleHudSnapshot(out BattleHudSnapshot snap)
    {
        snap = default;
        if (_battleWorld == null || CurrentStatus != E_BattleStatus.InBattle)
            return false;

        int score = GlobalBattleData.SessionScore;
        int hi = PlayerPrefs.GetInt("BattleHiScore", 0);

        var em = _battleWorld.EntityManager;
        Span<int> playerIndices = em.GetActiveIndices<CPlayer>();
        if (playerIndices.Length == 0)
        {
            snap = new BattleHudSnapshot(score, hi, 0, 0, 0);
            return true;
        }

        int chosen = playerIndices[0];
        byte localIdx = RoomManager.LocalPlayerIndex;
        for (int i = 0; i < playerIndices.Length; i++)
        {
            int idx = playerIndices[i];
            ref readonly var p = ref em.GetComponentSpan<CPlayer>()[idx];
            if (p.playerIndex == localIdx)
            {
                chosen = idx;
                break;
            }
        }

        ref readonly var pl = ref em.GetComponentSpan<CPlayer>()[chosen];
        ref readonly var hp = ref em.GetComponentSpan<CHealth>()[chosen];
        snap = new BattleHudSnapshot(score, hi, hp.currentHealth, hp.maxHealth, pl.powerOrbs);
        return true;
    }

    void TryBeginStageTimeline()
    {
        if (_battleWorld == null) return;
        var cfg = ResolveStageTimelineForBattle();
        if (cfg == null)
        {
            Logger.Warn($"[Battle] Stage timeline not resolved (id='{_activeStageTimelineConfigId}'). Register in GameResourceManifest.stageTimelineConfigIds.", LogTag.Battle);
            return;
        }
        _battleWorld.GetSystem<StageTimelineSystem>()?.Begin(cfg);
    }

    void PrepareBattleInfrastructure()
    {
        EnsureGlobalBattleDataReady();
        var globalPoolConfig = GameResDB.Instance.GetConfig<GlobalPoolConfig>("defaultglobalpool");
        WarmupGlobalPools(globalPoolConfig);
    }

    void WarmupGlobalPools(GlobalPoolConfig globalPoolConfig)
    {
        if (globalPoolConfig == null)
        {
            Logger.Warn("[Battle] GlobalPoolConfig 'defaultglobalpool' not found.", LogTag.Pool);
            return;
        }

        int maxPrefabIndex = GameResDB.Instance.GetMaxPrefabIndex();
        GameObjectPoolManager.Instance.Initialize(maxPrefabIndex);

        for (int i = 0; i < globalPoolConfig.poolCategories.Length; i++)
        {
            var categoryGroup = globalPoolConfig.poolCategories[i];
            for (int j = 0; j < categoryGroup.entries.Length; j++)
            {
                var entry = categoryGroup.entries[j];
                GameObjectPoolManager.Instance.WarmupPool(entry.prefabId, entry.defaultWarmupCount);
            }
        }
    }

    #region 玩家与测试

    public void AddPlayerData(PlayerBattleData playerData)
    {
        allPlayerDatas.Add(playerData);
        _activePlayers[playerData.playerIndex] = true;
    }

    /// <summary>准备阶段重复确认时按 playerIndex 覆盖，避免重复条目。</summary>
    public void SetOrUpdatePlayerData(PlayerBattleData playerData)
    {
        if (playerData.characterId != E_Character.None)
            _prepareCharacterRegistry.TryClaim(playerData.playerIndex, playerData.characterId);

        for (int i = 0; i < allPlayerDatas.Count; i++)
        {
            if (allPlayerDatas[i].playerIndex != playerData.playerIndex)
                continue;
            allPlayerDatas[i] = playerData;
            _activePlayers[playerData.playerIndex] = true;
            NotifyPrepareCharacterLocked(playerData.playerIndex, playerData.characterId);
            return;
        }
        AddPlayerData(playerData);
        NotifyPrepareCharacterLocked(playerData.playerIndex, playerData.characterId);
    }

    void NotifyPrepareCharacterLocked(byte playerIndex, E_Character characterId)
    {
        if (characterId == E_Character.None)
            return;
        OnPrepareCharacterLocked?.Invoke(playerIndex, characterId);
    }

    public void GeneratePlayer()
    {
        if (allPlayerDatas == null || allPlayerDatas.Count == 0)
        {
            Logger.Error("No player data available to create players.");
            return;
        }

        for (int i = 0; i < allPlayerDatas.Count; i++)
        {
            var playerData = allPlayerDatas[i];

            if (playerData.characterId == E_Character.None || playerData.weaponId == E_Weapon.None)
            {
                Logger.Error($"Invalid player data for player index {playerData.playerIndex}: characterId={playerData.characterId}, weaponId={playerData.weaponId}");
                continue;
            }
            InitializePlayerEntity(playerData);
        }
    }

    void InitializePlayerEntity(PlayerBattleData playerData)
    {
        var bornPos = GlobalBattleData.SpawnData.GetPlayerSpawnPos(playerData.playerIndex, TotalPlayers);
        Logger.Debug($"Spawning player {playerData.playerIndex} at position ({bornPos.x}, {bornPos.y})");
        var e_Player = _battleWorld.EntityFactory.CreatePlayer(playerData, bornPos.x, bornPos.y);
        _battleWorld.EntityManager.AddComponent(e_Player, new CPoolGetTag());
    }

    public void AddEnemyTest(EnemyConfig enemyConfig, float posX, float posY)
    {
        var e_enemy = _battleWorld.EntityFactory.CreateEnemy(enemyConfig, posX, posY);
        uint f = _battleWorld.LogicFrameTimer.CurrentFrame;
        _battleWorld.EntityManager.AddComponent(e_enemy, EnemyMovementBaking.CreateSimpleDescent(f, posX, posY));
        _battleWorld.EntityManager.AddComponent(e_enemy, new CPoolGetTag());
        Logger.Info($"Test enemy added at ({posX}, {posY}) with config index {enemyConfig.emitterConfigIndex}.");
    }

    #endregion

    void Update()
    {
        if (_battleWorld == null) return;
        if (CurrentStatus != E_BattleStatus.InBattle) return;

        _battleWorld.LogicFrameTimer.AccumulateDeltaTime(Time.unscaledDeltaTime);

        bool logicStalled = false;
        if (_battleWorld.LogicFrameTimer.CanAdvance())
        {
            uint frameToProcess = _battleWorld.LogicFrameTimer.CurrentFrame;

            FrameInput input = InputManager.Instance.RecordLocalInput(RoomManager.LocalPlayerIndex, frameToProcess);

            if (!isSinglePlayerMode)
                InputManager.Instance.BroadcastLocalInput(input);

            if (isSinglePlayerMode || InputManager.Instance.AreAllInputsReady(frameToProcess, _activePlayers))
            {
                _battleWorld.LogicTick(frameToProcess);
                _battleWorld.LogicFrameTimer.AdvanceFrame();
                _battleWorld.LogicFrameTimer.ConsumeFrameTime();
            }
            else
            {
                logicStalled = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Logger.Debug($"[Frame {frameToProcess}] Time ready but inputs not ready.");
#endif
            }
        }

        _battleWorld.SetPresentationLogicStalled(logicStalled);
        _battleWorld.Update(Time.deltaTime);
    }

    void LateUpdate()
    {
        _battleWorld?.LateUpdate(Time.deltaTime);
    }
}
