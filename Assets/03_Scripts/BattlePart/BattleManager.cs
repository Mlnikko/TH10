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
    public GameObject prefab;

    public BattleStatus CurrentStatus { get; private set; } = BattleStatus.Prepare;
    public List<PlayerBattleData> allPlayerDatas = new();

    bool[] activePlayers = new bool[4];

    int totalPlayers => allPlayerDatas.Count;

    World battleWorld;

    protected override void OnSingletonInit()
    {
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
            await ConfigManager.PreloadConfigsAsync<CharacterConfig>(ConfigHelper.allCharCfgIds);
            await ConfigManager.PreloadConfigsAsync<WeaponConfig>(ConfigHelper.allWeapCfgIds);

            GlobalBattleData.Initialize(await ConfigManager.GetConfigAsync<BattleAreaConfig>(ConfigHelper.BattleAreaCfgId));

            _isResourcesPreloaded = true;
            Logger.Info("Battle resources preloaded.");
        }
        catch (Exception ex)
        {
            Logger.Exception(ex);
            throw;
        }
    }

    #region 初始化
    void InitBattleWorld()
    {
        battleWorld = new World();

        battleWorld.AddSystem<LifetimeSystem>();

        battleWorld.AddSystem<CollisionSystem>();

        battleWorld.AddSystem<PlayerControlSystem>();

        Logger.Info("Battle ECS World initialized.");
    }
    #endregion

    #region 战斗启动调用
    public void StartMutiPlayerBattleForClient(uint startFrame, uint randomSeed, PlayerBattleData[] playerBattleDatas)
    {
        isSinglePlayerMode = false;

        foreach (var playerBattleData in playerBattleDatas)
        {
            AddPlayerData(playerBattleData);
        }

        GeneratePlayer();
        CurrentStatus = BattleStatus.InBattle;
    }

    public void StartMutiPlayerBattleForHost()
    {
        isSinglePlayerMode = false;
        GeneratePlayer();
        CurrentStatus = BattleStatus.InBattle;
    }

    public void StartSinglePlayerBattle()
    {
        isSinglePlayerMode = true;

        GeneratePlayer();
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
            InitializePlayerEntity(playerData, prefab);
        }
    }

    void InitializePlayerEntity(PlayerBattleData playerData, GameObject playerPrefab)
    {
        var bornPos = GlobalBattleData.SpawnData.GetPlayerSpawnPos(playerData.playerIndex, totalPlayers);
        var playerGO = Instantiate(playerPrefab);
        playerGO.transform.position = (Vector3)bornPos;

        var em = battleWorld.EntityManager;
        var playerEntity = em.CreateEntity();

        int gameObjectId = GameObjectBridge.Register(playerGO, new PlayerUpdater(playerGO));
        var characterConfig = ConfigManager.GetConfig<CharacterConfig>(playerData.characterId.ToString());
        if (characterConfig == null)
        {
            Logger.Error($"CharacterConfig not found for ID: {playerData.characterId}");
            // 可选：销毁已实例化的 GameObject
            GameObject.Destroy(playerGO);
            return;
        }

        #region 添加组件

        em.AddComponent(playerEntity, new CPresentationLink
        {
            presentationId = gameObjectId
        });

        em.AddComponent(playerEntity, new CPosition
        {
            x = bornPos.x,
            y = bornPos.y
        });

        em.AddComponent(playerEntity, new CVelocity
        {
            vx = 0,
            vy = 0
        });

        em.AddComponent(playerEntity, new CPlayer
        {
            playerIndex = playerData.playerIndex,
            characterId = (byte)playerData.characterId,
            weaponId = (byte)playerData.weaponId,
        });

        em.AddComponent(playerEntity, new CPlayerRunTime
        {
            isSlowMode = false,
        });

        em.AddComponent(playerEntity, new CPlayerAttribute
        {
            grazeRadius = characterConfig.GrazeRadius,
            hitRadius = characterConfig.HitRadius,
            moveSlowSpeed = characterConfig.MoveSlowSpeed,
            moveSpeed = characterConfig.MoveSpeed,
        });

        #endregion

        Logger.Info($"Player {playerData.playerIndex} ({playerData.characterId}) initialized successfully.", LogTag.Battle);
    }

    void FixedUpdate()
    {
        // 只有在战斗中才执行逻辑帧更新
        if (battleWorld == null) return;

        if (CurrentStatus == BattleStatus.InBattle)
        {
            uint currentFrame = LogicTimer.CurrentLogicFrame;

            InputManager.Instance.RecordAndBroadcastLocalInput(RoomManager.LocalPlayerIndex, currentFrame);

            // TODO ：本地预测与回滚机制

            // 3. 检查是否所有输入就绪
            if (!InputManager.Instance.AreAllInputsReady(currentFrame, activePlayers)) return;

            // 4. 推进 ECS 逻辑
            battleWorld?.FixedUpdate(LogicTimer.LOGIC_DELTA_TIME);

            // 6. 帧递增（进入下一逻辑帧）
            LogicTimer.AdvanceLogicFrame();

            // 7. 清理旧帧
            InputManager.Instance.CleanupOldFrames(currentFrame);
        }
    }

    // Update 和 LateUpdate 主要用于处理非核心逻辑（如渲染等）
    void Update()
    {
        battleWorld?.Update(Time.deltaTime);
    }

    void LateUpdate()
    {
        battleWorld?.LateUpdate(Time.deltaTime);
    }
}
