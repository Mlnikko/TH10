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
/// 战斗数据
/// </summary>
public struct BattleData
{
    public E_Rank rank;
    public PlayerData[] playerBattleDatas;
}

public struct PlayerData
{
    public byte playerIndex;
    public E_Character characterId;
    public E_Weapon weaponId;
}

public class BattleManager : SingletonMono<BattleManager>
{
    [SerializeField] GameObject playerPrefab;

    public BattleData battleData = new();

    bool[] _activePlayers = new bool[4];

    World battleWorld;

    [SerializeField] Transform playerRoot;
    [SerializeField] BattleArea battleArea;

    protected override void OnSingletonInit()
    {
        InitBattleWorld();

        var testPlayer0 = new PlayerData() { playerIndex = 0 , characterId = E_Character.Reimu};
        var testPlayer1 = new PlayerData() { playerIndex = 1 , characterId = E_Character.Marisa};

        CreatePlayer(testPlayer0);
        CreatePlayer(testPlayer1);

        AddPlayer(testPlayer0.playerIndex);
        AddPlayer(testPlayer1.playerIndex);
    }

    public void StartBattle()
    {
        Debug.Log("Battle Start!");
        SceneManager.LoadScene("BattleScene");
        SceneManager.UnloadSceneAsync("TitleScene");
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

    public void CreatePlayer(PlayerData playerData)
    {
        var playerGO = Instantiate(playerPrefab, playerRoot);

        var em = battleWorld.EntityManager;

        var playerEntity = em.CreateEntity();

        int presentationId = PresentationBridge.Register(playerGO, new PlayerPresentationUpdater(playerGO));

        var characterConfig = ConfigManager.Get<CharacterConfig>(playerData.characterId.ToString());

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
    }

    bool IsLocalPlayer(int playerIndex)
    {
        // Host 控制 P0，Client 控制 P1/P2/P3
        if (NetworkManager.Instance.netRole == NetworkRole.Host)
            return playerIndex == 0; // Host 是 P0

        if (NetworkManager.Instance.netRole == NetworkRole.Client)
            return playerIndex == NetworkManager.Instance.LocalPlayerIndex; // Client 控制自己的角色

        return false;
    }

    public void AddPlayer(byte playerIndex)
    {
        if (playerIndex < 4)
            _activePlayers[playerIndex] = true;
    }

    #region 核心更新循环
    void FixedUpdate()
    {
        uint currentFrame = GameTimeManager.CurrentLogicFrame;

        InputManager.Instance.RecordLocalInput(0, currentFrame);

        // 2. 接收网络事件（包括 InputMSG、断线等）
        NetworkManager.Instance.PollNetwork();

        // 3. 检查是否所有输入就绪
        if (!InputManager.Instance.AreAllInputsReady(currentFrame, _activePlayers)) return;

        // 4. 推进 ECS 逻辑（使用 currentFrame 的输入）
        battleWorld?.FixedUpdate(Time.fixedDeltaTime);

        // 6. 帧递增（进入下一逻辑帧）
        GameTimeManager.AdvanceLogicFrame();

        // 7. 清理旧帧
        InputManager.Instance.CleanupOldFrames(GameTimeManager.CurrentLogicFrame);
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
}
