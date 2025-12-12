using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public struct PlayerData
{
    public byte playerIndex;
    public E_Character characterId;
    public E_Weapon weaponId;

    public PlayerData(byte index, E_Character character, E_Weapon weapon)
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

public class BattleManager : SingletonMono<BattleManager>
{
    public BattleStatus CurrentStatus { get; private set; } = BattleStatus.Prepare;
    List<PlayerData> playerDatas = new List<PlayerData>();

    bool[] _activePlayers = new bool[4];

    World battleWorld;
    

    [SerializeField] Transform playerRoot;
    [SerializeField] BattleArea battleArea;


    protected override void OnSingletonInit()
    {
        BattleInit().Forget();
    }

    async Task BattleInit()
    {
        try
        {
  
            await ConfigManager.PreloadConfigsAsync<CharacterConfig>(ConfigHelper.allCharCfgIds);

            await ConfigManager.PreloadConfigsAsync<WeaponConfig>(ConfigHelper.allWeapCfgIds);

            //await SpriteManager

            await UIManager.Instance.ShowPanelAsync<BattlePreparePanel>();

            CurrentStatus = BattleStatus.Prepare;
        }
        catch (Exception ex)
        {
            Logger.Exception(ex);
        }
    }


    public void StartBattle()
    {
        if (playerDatas.Count == 0)
        {
            Logger.Error("Cannot start battle: No players added.");
            return;
        }

        // 初始化战斗世界
        InitBattleWorld();

        // 创建玩家实体
        CreatePlayer(); // 或 CreatePlayersAsync() 如果使用异步版本

        // 可以在这里添加战斗开始逻辑
        Logger.Info($"Battle started with {playerDatas.Count} players");
    }

    public void AddPlayer(PlayerData playerData)
    {
        playerDatas.Add(playerData);
    }

    void InitBattleWorld()
    {
        battleWorld = new World();
        
        var movementSys = battleWorld.AddSystem<MovementSystem>();
        
        var lifetimeSys = battleWorld.AddSystem<LifetimeSystem>();

        var collisionSys = battleWorld.AddSystem<CollisionSystem>();
        collisionSys.Initialize(battleArea.InitBattleArea());

        var playerControlSys = battleWorld.AddSystem<PlayerControlSystem>();
    }

    public void CreatePlayer()
    {
        //if (playerDatas == null || playerDatas.Count == 0)
        //{
        //    Logger.Error("No player data available to create players.");
        //    return;
        //}

        //foreach (var playerData in playerDatas)
        //{
        //    string characterPrefabPath = ResourceKeys.CharacterPrefabPath + playerData.characterId.ToString();

        //    // 异步加载角色预制体
        //    ResManager.LoadAssetAsync<GameObject>(characterPrefabPath, (playerPrefab) =>
        //    {
        //        if (playerPrefab == null)
        //        {
        //            Logger.Error($"Failed to load prefab for character: {playerData.characterId}");
        //            return;
        //        }

        //        // 在回调中完成初始化
        //        InitializePlayerEntity(playerData, playerPrefab);
        //    });
        //}
    }

    Vector2 GetStartPosition(byte playerIndex)
    {
        // 根据玩家索引设置不同的初始位置
        return playerIndex switch
        {
            0 => new Vector2(-3, 0),  // 左下
            1 => new Vector2(3, 0),   // 右下
            2 => new Vector2(-3, 3),  // 左上
            3 => new Vector2(3, 3),   // 右上
            _ => Vector2.zero
        };
    }

    void InitializePlayerEntity(PlayerData playerData, GameObject playerPrefab)
    {
        var playerGO = Instantiate(playerPrefab, playerRoot);

        var em = battleWorld.EntityManager;
        var playerEntity = em.CreateEntity();

        int presentationId = PresentationBridge.Register(playerGO, new PlayerPresentationUpdater(playerGO));
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
            presentationId = presentationId
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
            playerIndex = playerData.playerIndex,
            grazeRadius = characterConfig.GrazeRadius,
            hitRadius = characterConfig.HitRadius,
            moveSlowSpeed = characterConfig.MoveSlowSpeed,
            moveSpeed = characterConfig.MoveSpeed,
            isSlowMode = false,
        });

        #endregion

        Logger.Info($"Player {playerData.playerIndex} ({playerData.characterId}) initialized successfully.");
    }

    void FixedUpdate()
    {
        // 只有在战斗中才执行逻辑帧更新
        if (CurrentStatus != BattleStatus.InBattle || battleWorld == null) return;

        uint currentFrame = GameTimeManager.CurrentLogicFrame;

        InputManager.Instance.RecordLocalInput(0, currentFrame);

        // 3. 检查是否所有输入就绪
        if (!InputManager.Instance.AreAllInputsReady(currentFrame, _activePlayers)) return;

        // 4. 推进 ECS 逻辑（使用 currentFrame 的输入）
        battleWorld?.FixedUpdate(Time.fixedDeltaTime);

        // 6. 帧递增（进入下一逻辑帧）
        GameTimeManager.AdvanceLogicFrame();

        // 7. 清理旧帧
        InputManager.Instance.CleanupOldFrames(GameTimeManager.CurrentLogicFrame);
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
