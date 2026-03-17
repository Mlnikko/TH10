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

public enum BattleStatus
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
    bool _isResourcesPreloaded = false;

    public bool isSinglePlayerMode;

    public BattleStatus CurrentStatus { get; private set; } = BattleStatus.Prepare;
    public List<PlayerBattleData> allPlayerDatas = new(4);

    bool[] activePlayers = new bool[4];

    int TotalPlayers => allPlayerDatas.Count;

    World _battleWorld;

    public async Task EnterBattleScene()
    {
        if (_isResourcesPreloaded) return;
        try
        {
            var battleAreaConfig = await ResManager.Instance.LoadAsync<BattleAreaConfig>(E_ResourceCategory.Config,"DefaultBattleArea");

            if (battleAreaConfig != null)
            {
                GlobalBattleData.Initialize(battleAreaConfig);
            }
            else
            {
                Logger.Critical("Failed to load BattleAreaConfig.", LogTag.Config);
            }

            _isResourcesPreloaded = true;
            Logger.Info("Battle resources preloaded.");
        }
        catch
        {
            Logger.Critical("Error during battle resources preloading: ", LogTag.Battle);
        }
        UIManager.Instance.ShowPanelAsync<BattlePreparePanel>().Forget();
    }

    #region 初始化
    void PerpareBattleWorld()
    {
        _battleWorld = new World();

        _battleWorld.AddSystem<LifetimeSystem>();

        _battleWorld.AddSystem<CollisionSystem>();

        _battleWorld.AddSystem<PlayerControlSystem>();

        _battleWorld.AddSystem<DanmakuSystem>();

        _battleWorld.AddSystem<DanmakuEmitSystem>();

        _battleWorld.AddSystem<PresentationSystem>();

        Logger.Info("Battle ECS World initialized.");
    }

    void WarmupDanmakuPool()
    {
        int maxPrefabIndex = GameResDB.Instance.GetMaxPrefabIndex();
        GameObjectPoolManager.Instance.Initialize(maxPrefabIndex);

        var danmakuCfgs = GameResDB.Instance.GetConfigs<DanmakuConfig>();
        foreach (var config in danmakuCfgs)
        {
            GameObjectPoolManager.Instance.WarmupPool(config.danmakuPrefabIndex, config.poolSize);
        }
    }

    #endregion

    #region 战斗启动调用

    #region 怪物相关

    public void AddEnemyTest(EnemyConfig enemyConfig, float posX, float posY)
    {
        _battleWorld.EntityFactory.CreateEnemy(enemyConfig, posX, posY);
        Logger.Info($"Test enemy added at ({posX}, {posY}) with config index {enemyConfig.emitterConfigIndex}.");
    }

    #endregion
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
        EnterBattleScene().Forget();
        PerpareBattleWorld();
        WarmupDanmakuPool();
        GeneratePlayer();
        CurrentStatus = BattleStatus.InBattle;
    }

    void StartMutiPlayerBattle(uint startFrame, uint randomSeed, PlayerBattleData[] playerDatas)
    {
        isSinglePlayerMode = false;

        // 2. 初始化玩家数据
        allPlayerDatas.Clear();

        foreach (var data in playerDatas)
        {
            AddPlayerData(data);
        }
    
        EnterBattleScene().Forget();
        // 3. 生成角色（ECS 实体等）
        PerpareBattleWorld();
        WarmupDanmakuPool();

        GeneratePlayer();

        _battleWorld.LogicFrameTimer.ResetToFrame(startFrame);

        // 5. 标记进入战斗
       
        CurrentStatus = BattleStatus.InBattle;   
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
        if (CurrentStatus != BattleStatus.InBattle) return;

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
