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

public static class BattleTimer
{
    public const float LOGIC_DELTA_TIME = 0.02f;
    public static uint CurrentLogicFrame { get; private set; }
    public static void AdvanceLogicFrame() => CurrentLogicFrame++;
}

public class BattleManager : SingletonMono<BattleManager>
{
    public bool isSinglePlayerMode;
    public GameObject prefab;

    public BattleStatus CurrentStatus { get; private set; } = BattleStatus.Prepare;
    public List<PlayerBattleData> allPlayerDatas = new();

    bool[] activePlayers = new bool[4];

    World battleWorld;
    

    [SerializeField] Transform playerRoot;
    [SerializeField] BattleArea battleArea;


    protected override void OnSingletonInit()
    {
        PreloadAndSceneInit().Forget();
        InitBattleWorld();
    }

    async Task PreloadAndSceneInit()
    {
        try
        {
            await ConfigManager.PreloadConfigsAsync<CharacterConfig>(ConfigHelper.allCharCfgIds);

            await ConfigManager.PreloadConfigsAsync<WeaponConfig>(ConfigHelper.allWeapCfgIds);

            await UIManager.Instance.ShowPanelAsync<BattlePreparePanel>();

            CurrentStatus = BattleStatus.Prepare;
        }
        catch (Exception ex)
        {
            Logger.Exception(ex);
        }
    }

    void InitBattleWorld()
    {
        battleWorld = new World();

        battleWorld.AddSystem<MovementSystem>();

        battleWorld.AddSystem<LifetimeSystem>();

        battleWorld.AddSystem<CollisionSystem>().Initialize(battleArea.InitBattleArea());

        battleWorld.AddSystem<PlayerControlSystem>();

        Logger.Info("Battle ECS World initialized.");
    }

    public void StartMutiPlayerBattleForClient(uint startFrame, uint randomSeed, PlayerBattleData[] playerBattleDatas)
    {
        isSinglePlayerMode = false;

        foreach (var playerBattleData in playerBattleDatas)
        {
            AddPlayer(playerBattleData);
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

    public void AddPlayer(PlayerBattleData playerData)
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

    Vector2 GetPlayerBornPos(byte playerIndex)
    {
        // 根据玩家索引设置不同的初始位置
        return playerIndex switch
        {
            0 => new Vector2(-3, 0),  // 左下
            1 => new Vector2(3, 0),   // 右下
            2 => new Vector2(-3, 0),  // 左上
            3 => new Vector2(3, 0),   // 右上
            _ => Vector2.zero
        };
    }

    void InitializePlayerEntity(PlayerBattleData playerData, GameObject playerPrefab)
    {
        var bornPos = GetPlayerBornPos(playerData.playerIndex);
        var playerGO = Instantiate(playerPrefab, playerRoot);
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
            x = playerRoot.position.x,
            y = playerRoot.position.y
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
            uint currentFrame = BattleTimer.CurrentLogicFrame;

            InputManager.Instance.RecordAndBroadcastLocalInput(RoomManager.LocalPlayerIndex, currentFrame);

            // 3. 检查是否所有输入就绪
            if (!InputManager.Instance.AreAllInputsReady(currentFrame, activePlayers)) return;

            // 4. 推进 ECS 逻辑
            battleWorld?.FixedUpdate(BattleTimer.LOGIC_DELTA_TIME);

            // 6. 帧递增（进入下一逻辑帧）
            BattleTimer.AdvanceLogicFrame();

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
