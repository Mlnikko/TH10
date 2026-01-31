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
    public List<PlayerBattleData> allPlayerDatas = new();

    bool[] activePlayers = new bool[4];

    int TotalPlayers => allPlayerDatas.Count;

    LogicFrameTimer logicTimer;
    World battleWorld;

    protected override void OnSingletonInit()
    {
        logicTimer = new LogicFrameTimer();
        InitBattleWorld();
    }

    async void Start()
    {
        await PreloadBattleResourcesAsync();
        UIManager.Instance.ShowPanelAsync<BattlePreparePanel>().Forget();
    }

    public async Task PreloadBattleResourcesAsync()
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
    }

    #region 初始化
    void InitBattleWorld()
    {
        battleWorld = new World();

        battleWorld.AddSystem<LifetimeSystem>();

        battleWorld.AddSystem<CollisionSystem>();

        battleWorld.AddSystem<PlayerControlSystem>();

        battleWorld.AddSystem<DanmakuSystem>();

        battleWorld.AddSystem<DanmakuEmitSystem>();

        Logger.Info("Battle ECS World initialized.");
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

        // 3. 生成角色（ECS 实体等）
        GeneratePlayer();

        logicTimer.ResetToFrame(startFrame);

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

        foreach (var playerData in allPlayerDatas)
        {
            InitializePlayerEntity(playerData);
        }
    }

    void InitializePlayerEntity(PlayerBattleData playerData)
    {
        
        string characterIdStr = playerData.characterId.ToString().ToLowerInvariant();
        var characterConfig = GameResDB.GetConfig<CharacterConfig>(characterIdStr);

        if (characterConfig == null)
        {
            Logger.Error($"CharacterConfig not found for Key: {characterIdStr}");
            return;
        }

        var bornPos = GlobalBattleData.SpawnData.GetPlayerSpawnPos(playerData.playerIndex, TotalPlayers);

        string charPrefabId = playerData.characterId.ToString().ToLowerInvariant();
        var characterPrefab = GameResDB.GetPrefab(charPrefabId);

        if (characterPrefab == null)
        {
            Logger.Error($"Character prefab not found for ID: {charPrefabId}");
            return;
        }

        var playerGO = Instantiate(characterPrefab);

        playerGO.transform.position = (Vector3)bornPos;

        var em = battleWorld.EntityManager;
        var playerEntity = em.CreateEntity();

        int gameObjectId = GameObjectBridge.Register(playerGO, new PlayerUpdater(playerGO));

        #region 添加组件

        em.AddComponent(playerEntity, new CGameObjectLink
        {
            gid = gameObjectId
        });

        em.AddComponent(playerEntity, new CPosition(bornPos.x, bornPos.y));
        em.AddComponent(playerEntity, new CVelocity(0, 0));
        em.AddComponent(playerEntity, new CPlayer(playerData.playerIndex, (byte)playerData.characterId, (byte)playerData.weaponId));
        em.AddComponent(playerEntity, new CPlayerRunTime
        {
            isSlowMode = false,
        });

        string weaponIdStr = playerData.weaponId.ToString().ToLowerInvariant();
        var weaponConfig = GameResDB.GetConfig<WeaponConfig>(weaponIdStr);

        foreach (var emitterId in weaponConfig.danmakuEmitterConfigIds)
        {
            //em.AddComponent(playerEntity, new CDanmakuEmitter(true, GameResDB.GetIndexById(emitterId)));
        }


        em.AddComponent(playerEntity, characterConfig.ToRuntimeAttribute(logicTimer.FrameInterval));

        #endregion

        Logger.Info($"Player {playerData.playerIndex} ({playerData.characterId}) initialized successfully.", LogTag.Battle);
    }


    // Update 和 LateUpdate 主要用于处理非核心逻辑（如渲染等）
    void Update()
    {
        if (battleWorld == null) return;
        if (CurrentStatus != BattleStatus.InBattle) return;

        // 累积时间（用于控制帧率）
        logicTimer.AccumulateDeltaTime(Time.unscaledDeltaTime);

        // 【关键】只有时间到了，才尝试处理 CurrentFrame
        if (logicTimer.CanAdvance()) // 即 accumulated >= frameInterval
        {
            uint frameToProcess = logicTimer.CurrentFrame;

            FrameInput input = InputManager.Instance.RecordLocalInput(RoomManager.LocalPlayerIndex, frameToProcess);

            if (!isSinglePlayerMode)
            {
                InputManager.Instance.BroadcastLocalInput(input);
            }

            // 检查是否所有满足帧推进条件（单人模式 或 多人模式输入就绪）
            if (isSinglePlayerMode || InputManager.Instance.AreAllInputsReady(frameToProcess, activePlayers))
            {
                // 执行逻辑
                battleWorld.LogicTick(frameToProcess);

                // 推进到下一帧（现在 CurrentFrame 表示“下一个要处理的帧”）
                logicTimer.AdvanceFrame(); // CurrentFrame++

                // 消耗时间
                logicTimer.ConsumeFrameTime();

                //// 清理旧输入
                InputManager.Instance.CleanupOldFrames(frameToProcess);
            }
            else
            {
                // 时间到了但输入没齐 → 卡住（正常行为，等待网络）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Frame {frameToProcess}] Time ready but inputs not ready.");
#endif
            }

        }

        // 8. 表现层更新（每帧都执行！）
        battleWorld?.Update(Time.deltaTime);
    }

    void LateUpdate()
    {
        battleWorld?.LateUpdate(Time.deltaTime);
    }
}
