using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// 战斗区域配置
/// </summary>
[Serializable]
public struct BattleAreaData : IEquatable<BattleAreaData>
{
    // === 战斗区域 ===
    public float Width;          // 战斗区域宽度（单位：Unity 世界单位）
    public float Height;         // 战斗区域高度
    public Vector2 Center;       // 战斗区域中心点（通常为 (0,0)）

    // === 网格加速参数 ===
    public int GridCellSize;     // 网格单元格尺寸（建议 64 或 128，正方形）

    // === 弹幕回收边界（外扩区域）===
    public Vector2 DanmakuRecycleMargin; // 超出战斗区域多少距离后回收弹幕

    // === 辅助属性（只读，计算得来）===

    public float Left => Center.x - Width * 0.5f;
    public float Right => Center.x + Width * 0.5f;
    public float Bottom => Center.y - Height * 0.5f;
    public float Top => Center.y + Height * 0.5f;

    public Rect BattleRect => new Rect(Left, Bottom, Width, Height);

    // 回收区域边界（用于销毁远距离弹幕）
    public float RecycleLeft => Left - DanmakuRecycleMargin.x;
    public float RecycleRight => Right + DanmakuRecycleMargin.x;
    public float RecycleBottom => Bottom - DanmakuRecycleMargin.y;
    public float RecycleTop => Top + DanmakuRecycleMargin.y;

    // 用于 DeterministicGrid 的世界原点（左下角 - 边距）
    public Vector2 GridWorldOrigin => new Vector2(
        RecycleLeft - 50f,   // 额外安全边距
        RecycleBottom - 50f
    );

    // 总覆盖宽度/高度（用于计算网格大小）
    public float TotalWidth => Width + DanmakuRecycleMargin.x * 2f + 100f;
    public float TotalHeight => Height + DanmakuRecycleMargin.y * 2f + 100f;

    // 网格维度（向上取整）
    public int GridColumns => Mathf.CeilToInt(TotalWidth / GridCellSize);
    public int GridRows => Mathf.CeilToInt(TotalHeight / GridCellSize);

    public BattleAreaData(float width, float height, Vector2 center, int cellSize = 64, Vector2 recycleMargin = default)
    {
        Width = width;
        Height = height;
        Center = center;
        GridCellSize = cellSize;
        DanmakuRecycleMargin = recycleMargin == default ? new Vector2(100, 100) : recycleMargin;
    }

    // 默认构造（避免未初始化）
    public static BattleAreaData Default => new BattleAreaData(1280, 720, Vector2.zero, 64, new Vector2(100, 100));

    // 用于帧同步一致性校验
    public bool Equals(BattleAreaData other)
    {
        return Width == other.Width &&
               Height == other.Height &&
               Center.Equals(other.Center) &&
               GridCellSize == other.GridCellSize &&
               DanmakuRecycleMargin.Equals(other.DanmakuRecycleMargin);
    }

    public override bool Equals(object obj) => obj is BattleAreaData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Width, Height, Center, GridCellSize, DanmakuRecycleMargin);
}

/// <summary>
/// 全局战斗数据
/// </summary>
public struct GlobalBattleData
{
    public E_Rank rank;
    public PlayerBattleData[] playerBattleDatas;
}

public struct PlayerBattleData
{
    public byte playerIndex;
    public E_Character characterId;
    public E_Weapon weaponId;

    public int playerEntityId; // ECS 中的实体 ID
    public int score;
    public int life;
    public int bomb;

    public override string ToString()
    {
        return $"Player ID: {playerIndex}, Character: {characterId}, Weapon: {weaponId}, Score: {score}, Life: {life}, Bomb: {bomb}";
    }
}

public class BattleManager : SingletonMono<BattleManager>
{
    [SerializeField] GameObject playerPrefab;

    public GlobalBattleData globalBattleData = new();
    BattleAreaData battleAreaData = new();

    World battleWorld;

    [SerializeField] Transform playerRoot;
    [SerializeField] BattleArea battleArea;

    protected override void OnSingletonInit()
    {
        EventManager.Instance.RegistEvent(E_Event.BattleStart, StartBattle);

        battleAreaData = battleArea.InitBattleArea();
        InitBattleWorld(globalBattleData);

        ConfigManager.PreloadAll<CharacterConfig>();
        //ConfigManager.PreloadAll<WeaponConfig>();

        var testPlayer = new PlayerBattleData() { playerIndex = 0 , characterId = E_Character.Reimu};
        Debug.Log("Creating Test Player: " + testPlayer);
        CreatePlayer(testPlayer);
    }

    public void StartBattle()
    {
        Debug.Log("Battle Start!");
        SceneManager.LoadScene("BattleScene");
        SceneManager.UnloadSceneAsync("TitleScene");
    }

    public void CreatePlayer(PlayerBattleData playerData)
    {
        var playerGO = Instantiate(playerPrefab, playerRoot);

        var em = battleWorld.EntityManager;

        var playerEntity = em.CreateEntity();


        // 创建玩家时（如你之前代码）
        var updater = new PlayerPresentationUpdater(playerGO);
        int presentationId = PresentationBridge.Register(playerGO, updater);

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

        CharacterConfig characterConfig = ConfigManager.Get<CharacterConfig>(playerData.characterId.ToString());

        em.AddComponent(playerEntity, new CPlayerRunTime
        {
            playerIndex = playerData.playerIndex,

            grazeRadius = characterConfig.GrazeRadius,
            hitRadius = characterConfig.HitRadius,
            
            moveSlowSpeed = characterConfig.MoveSlowSpeed,
            moveSpeed = characterConfig.MoveSpeed,

            isSlowMode = false,
        });
    }

    void InitBattleWorld(GlobalBattleData globalBattleData)
    {
        battleWorld = new World();
        
        var movementSys = battleWorld.AddSystem<MovementSystem>();
        
        var lifetimeSys = battleWorld.AddSystem<LifetimeSystem>();

        var collisionSys = battleWorld.AddSystem<CollisionSystem>();
        collisionSys.Initialize(battleAreaData);

        var playerControlSys = battleWorld.AddSystem<PlayerControlSystem>();
    }

    BitArray _activePlayers = new BitArray(4) { [0] = true }; // 单人测试

    #region 核心更新循环（帧同步）
    void FixedUpdate()
    {
        //if (!_isBattleRunning) return;

        ushort currentFrame = GameTimeManager.CurrentLogicFrame;

        // 1. 本地玩家采样（假设只有 P0 是本地）
        if (_activePlayers[0])
        {
            InputManager.Instance.RecordLocalInput(0, currentFrame);
        }

        // 2. 等待所有活跃玩家输入就绪
        if (!InputManager.Instance.AreAllInputsReady(currentFrame, _activePlayers))
        {
            return; // 等待下一帧（或加超时处理）
        }

        // 3. 推进 ECS 逻辑
        battleWorld?.FixedUpdate(Time.fixedDeltaTime);

        // 4. 帧递增
        GameTimeManager.AdvanceLogicFrame();
    }

    void Update()
    {
        battleWorld?.Update(Time.deltaTime);
    }

    void LateUpdate()
    {
        battleWorld?.LateUpdate(Time.deltaTime);
    }
    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventManager.Instance.UnRegistEvent(E_Event.BattleStart, StartBattle);     
    }
}
