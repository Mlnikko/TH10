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

public static class GlobalBattleData
{
    public static BattleAreaData AreaData { get; private set; }
    public static PlayerSpawnData SpawnData { get; private set; }

    public static bool IsInitialized { get; private set; }

    public static void Initialize(BattleAreaConfig config)
    {
        AreaData = config.battleAreaData;
        SpawnData = config.playerSpawnData;
        IsInitialized = true;
    }
}

public class BattleManager : SingletonMono<BattleManager>
{
    /// <summary>是否已通过 Addressables 至少加载过一次战斗区（避免重复下载）；开战时仍会按需从 GameResDB 同步。</summary>
    bool _battleAreaPreloadedFromAddressables;

    public bool isSinglePlayerMode;

    public E_BattleStatus CurrentStatus { get; private set; } = E_BattleStatus.Prepare;
    public List<PlayerBattleData> allPlayerDatas = new(4);

    bool[] activePlayers = new bool[4];

    int TotalPlayers => allPlayerDatas.Count;

    World _battleWorld;

    [Header("关卡时间线")]
    [Tooltip("StageTimelineConfig 的 ConfigId（SO 文件名小写）；运行时从 GameResDB 取，未进库则走 Addressables cfg_*")]
    [SerializeField] string _activeStageTimelineConfigId = "stagetimeline_1";

    /// <summary>Manifest 未登记时由 <see cref="EnsureStageTimelineReadyAsync"/> 从 Addressables 加载的缓存。</summary>
    StageTimelineConfig _runtimeAddressableTimeline;

    public string ActiveStageTimelineConfigId => _activeStageTimelineConfigId;

    /// <summary>
    /// 从主菜单进入战斗准备：可选首次从 Addressables 拉战斗区，打开准备面板。
    /// 真正开战在 <see cref="StartSinglePlayerBattle"/> / <see cref="StartMutiPlayerBattle"/>，与 UI 入口解耦。
    /// </summary>
    public async Task EnterBattleScene()
    {
        if (!_battleAreaPreloadedFromAddressables)
        {
            try
            {
                string areaId = ResolveBattleAreaConfigId();
                var battleAreaConfig = await ResManager.Instance.LoadAsync<BattleAreaConfig>(E_ResourceCategory.Config, areaId);
                if (battleAreaConfig != null)
                {
                    GlobalBattleData.Initialize(battleAreaConfig);
                    _battleAreaPreloadedFromAddressables = true;
                    Logger.Info($"Battle area preloaded (Addressables): '{areaId}'.", LogTag.Battle);
                }
                else
                    Logger.Critical($"Failed to load BattleAreaConfig '{areaId}'.", LogTag.Config);
            }
            catch (Exception ex)
            {
                Logger.Critical($"Error during battle area preload: {ex.Message}", LogTag.Battle);
            }
        }

        // 多人直连战斗等路径可能未走 Addressables，用 Manifest 里已进 GameResDB 的配置补齐
        SyncGlobalBattleAreaFromResDbIfNeeded();

        await UIManager.Instance.ShowPanelAsync<BattlePreparePanel>();
    }

    static string ResolveBattleAreaConfigId()
    {
        var manifest = ResManager.Instance != null ? ResManager.Instance.Manifest : null;
        if (manifest != null && !string.IsNullOrEmpty(manifest.battleAreaConfigId))
            return manifest.battleAreaConfigId;
        return "defaultbattlearea";
    }

    void DisposeBattleWorld()
    {
        if (_battleWorld == null) return;
        _battleWorld.GetSystem<StageTimelineSystem>()?.End();
        _battleWorld.Dispose();
        _battleWorld = null;
    }

    /// <summary>
    /// 下一关或重开时切换时间轴 id，并清除仅 Addressables 加载的缓存引用。
    /// </summary>
    public void SetActiveStageTimelineConfigId(string configId)
    {
        if (string.IsNullOrEmpty(configId)) return;
        _activeStageTimelineConfigId = configId.ToLowerInvariantTrimmed();
        _runtimeAddressableTimeline = null;
    }

    StageTimelineConfig ResolveStageTimelineForBattle()
    {
        if (GameResDB.IsInitialized)
        {
            var fromDb = GameResDB.Instance.GetConfig<StageTimelineConfig>(_activeStageTimelineConfigId);
            if (fromDb != null)
                return fromDb;
        }

        return _runtimeAddressableTimeline;
    }

    /// <summary>
    /// 完成一关后切换 Stage：按新配置 id 加载时间轴并重建 ECS 世界（保留 <see cref="allPlayerDatas"/>）。
    /// </summary>
    // public async Task<bool> RestartBattleWithStageAsync(string stageTimelineConfigId)
    // {
    //     if (string.IsNullOrEmpty(stageTimelineConfigId))
    //         return false;

    //     SetActiveStageTimelineConfigId(stageTimelineConfigId);

    //     BootstrapBattleSession(0);
    //     return true;
    // }

    /// <summary>查询当前关卡状态（时间线未 Begin 时为 false）。</summary>
    public bool TryGetStageState(out E_StageState state)
    {
        state = E_StageState.None;
        if (_battleWorld == null) return false;
        var timeline = _battleWorld.GetSystem<StageTimelineSystem>();
        return timeline != null && timeline.TryGetStageState(out state);
    }

    /// <summary>
    /// 建立 ECS 世界与系统（调用前应先 <see cref="DisposeBattleWorld"/>）。
    /// </summary>
    void CreateBattleWorld()
    {
        _battleWorld = new World();
        _battleWorld.AddSystem<StageTimelineSystem>();
        _battleWorld.AddSystem<EnemyMovementSystem>();
        _battleWorld.AddSystem<CollisionSystem>();
        _battleWorld.AddSystem<CollisionLogicSystem>();
        _battleWorld.AddSystem<PlayerControlSystem>();
        _battleWorld.AddSystem<DanmakuSystem>();
        _battleWorld.AddSystem<DanmakuEmitSystem>();
        _battleWorld.AddSystem<PresentationSystem>();
        Logger.Info("Battle ECS World initialized.");
    }

    /// <summary>
    /// 开战 / 切关 的统一顺序：准备 → 释放旧世界 → 战斗区+池 → 新世界 → 逻辑帧 → 时间轴 → 玩家 → InBattle。
    /// </summary>
    void BootstrapBattleSession(uint logicStartFrame)
    {
        CurrentStatus = E_BattleStatus.Prepare;
        DisposeBattleWorld();
        PrepareBattleInfrastructure();
        CreateBattleWorld();
        _battleWorld.LogicFrameTimer.ResetToFrame(logicStartFrame);
        TryBeginStageTimeline();
        GeneratePlayer();
        CurrentStatus = E_BattleStatus.InBattle;
    }

    void TryBeginStageTimeline()
    {
        if (_battleWorld == null) return;
        var cfg = ResolveStageTimelineForBattle();
        if (cfg == null)
        {
            Logger.Warn($"[Battle] Stage timeline not resolved (id='{_activeStageTimelineConfigId}'). Register in GameResourceManifest.stageTimelineConfigIds or ensure cfg_* Addressables entry.", LogTag.Battle);
            return;
        }
        var timeline = _battleWorld.GetSystem<StageTimelineSystem>();
        timeline?.Begin(cfg);
    }

    /// <summary>
    /// 与 ECS 世界无关的战斗环境：全局战斗区数据 + 对象池预热（依赖 GameResDB 已初始化）。
    /// </summary>
    void PrepareBattleInfrastructure()
    {
        SyncGlobalBattleAreaFromResDbIfNeeded();

        var globalPoolConfig = GameResDB.Instance.GetConfig<GlobalPoolConfig>("defaultglobalpool");
        WarmupGlobalPools(globalPoolConfig);
    }

    /// <summary>
    /// <see cref="EnterBattleScene"/> 为异步时，战斗可能在 GlobalBattleData 初始化前就开局；此处用 Manifest 中的战斗区配置立刻对齐。
    /// </summary>
    void SyncGlobalBattleAreaFromResDbIfNeeded()
    {
        if (GlobalBattleData.IsInitialized || !GameResDB.IsInitialized)
            return;

        var manifest = ResManager.Instance.Manifest;
        if (manifest == null || string.IsNullOrEmpty(manifest.battleAreaConfigId))
            return;

        var battleArea = GameResDB.Instance.GetConfig<BattleAreaConfig>(manifest.battleAreaConfigId);
        if (battleArea != null)
            GlobalBattleData.Initialize(battleArea);
    }

    void WarmupGlobalPools(GlobalPoolConfig globalPoolConfig)
    {
        int maxPrefabIndex = GameResDB.Instance.GetMaxPrefabIndex();
        GameObjectPoolManager.Instance.Initialize(maxPrefabIndex);
        
        for(int i = 0; i < globalPoolConfig.poolCategories.Length; i++)
        {
            var categoryGroup = globalPoolConfig.poolCategories[i];
            for(int j = 0; j < categoryGroup.entries.Length; j++)
            {
                var entry = categoryGroup.entries[j];
                GameObjectPoolManager.Instance.WarmupPool(entry.prefabId, entry.defaultWarmupCount);
            }
        }
    }

    #region 怪物相关

    public void AddEnemyTest(EnemyConfig enemyConfig, float posX, float posY)
    {
        var e_enemy = _battleWorld.EntityFactory.CreateEnemy(enemyConfig, posX, posY);
        uint f = _battleWorld.LogicFrameTimer.CurrentFrame;
        _battleWorld.EntityManager.AddComponent(e_enemy, EnemyMovementBaking.CreateSimpleDescent(f, posX, posY));
        _battleWorld.EntityManager.AddComponent(e_enemy, new CPoolGetTag());
        Logger.Info($"Test enemy added at ({posX}, {posY}) with config index {enemyConfig.emitterConfigIndex}.");
    }

    #endregion

    #region 战斗启动调用
    public void StartMutiPlayerBattleForClient(uint startFrame, uint randomSeed, PlayerBattleData[] allPlayerDatas)
    {
        StartMutiPlayerBattle(startFrame, randomSeed, allPlayerDatas);
    }
    public void StartMutiPlayerBattleForHost()
    {
        var allPlayerDatas = this.allPlayerDatas.ToArray();

        // 1. 生成全局一致的起始参数
        uint startFrame = 0;
        uint randomSeed = 0;

        // 2. 广播给所有客户端
        var startMsg = new BattleStartMSG
        {
            startFrame = startFrame,
            randomSeed = randomSeed,
            playerDatas = allPlayerDatas
        };
        NetworkManager.Instance.Broadcast(startMsg);

        // 3. 主机自己也初始化
        StartMutiPlayerBattle(startFrame, randomSeed, allPlayerDatas);
    }

    public void StartSinglePlayerBattle()
    {
        isSinglePlayerMode = true;
        // 准备面板与战斗区预载由菜单 <see cref="EnterBattleScene"/> 负责，此处只启动战斗会话
        BootstrapBattleSession(0);
    }

    void StartMutiPlayerBattle(uint startFrame, uint randomSeed, PlayerBattleData[] playerDatas)
    {
        isSinglePlayerMode = false;

        allPlayerDatas.Clear();
        foreach (var data in playerDatas)
            AddPlayerData(data);

        BootstrapBattleSession(startFrame);
    }
    #endregion

    public void AddPlayerData(PlayerBattleData playerData)
    {
        allPlayerDatas.Add(playerData);
        activePlayers[playerData.playerIndex] = true;
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

            if(playerData.characterId == E_Character.None || playerData.weaponId == E_Weapon.None)
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

    void Update()
    {
        if (_battleWorld == null) return;
        if (CurrentStatus != E_BattleStatus.InBattle) return;

        // 累积时间（用于控制帧率）
        _battleWorld.LogicFrameTimer.AccumulateDeltaTime(Time.unscaledDeltaTime);

        // 【关键】只有时间到了，才尝试处理 CurrentFrame
        if (_battleWorld.LogicFrameTimer.CanAdvance()) // 即 accumulated >= frameInterval
        {
            uint frameToProcess = _battleWorld.LogicFrameTimer.CurrentFrame;

            FrameInput input = InputManager.Instance.RecordLocalInput(RoomManager.LocalPlayerIndex, frameToProcess);

            if (!isSinglePlayerMode)
            {
                InputManager.Instance.BroadcastLocalInput(input);
            }

            // 检查是否所有满足帧推进条件（单人模式 或 多人模式输入就绪）
            if (isSinglePlayerMode || InputManager.Instance.AreAllInputsReady(frameToProcess, activePlayers))
            {
                // 执行逻辑
                _battleWorld.LogicTick(frameToProcess);

                // 推进到下一帧（现在 CurrentFrame 表示“下一个要处理的帧”）
                _battleWorld.LogicFrameTimer.AdvanceFrame(); // CurrentFrame++

                // 消耗时间
                _battleWorld.LogicFrameTimer.ConsumeFrameTime();

                // 清理旧输入
                //InputManager.Instance.CleanupOldFrames(frameToProcess);
            }
            else
            {
                // 时间到了但输入没齐 → 卡住（正常行为，等待网络）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Logger.Debug($"[Frame {frameToProcess}] Time ready but inputs not ready.");
#endif
            }

        }

        _battleWorld?.Update(Time.deltaTime);
    }

    void LateUpdate()
    {
        _battleWorld?.LateUpdate(Time.deltaTime);
    }
}
