using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// 战斗区域配置（纯数据，用于帧同步）
/// 所有字段必须是确定性类型（int/float/Vector2 等），且初始化方式一致
/// </summary>
[Serializable]
public struct BattleAreaConfig : IEquatable<BattleAreaConfig>
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

    // 构造函数（方便代码创建）
    public BattleAreaConfig(float width, float height, Vector2 center, int cellSize = 64, Vector2 recycleMargin = default)
    {
        Width = width;
        Height = height;
        Center = center;
        GridCellSize = cellSize;
        DanmakuRecycleMargin = recycleMargin == default ? new Vector2(100, 100) : recycleMargin;
    }

    // 默认构造（避免未初始化）
    public static BattleAreaConfig Default => new BattleAreaConfig(1280, 720, Vector2.zero, 64, new Vector2(100, 100));

    // 用于帧同步一致性校验
    public bool Equals(BattleAreaConfig other)
    {
        return Width == other.Width &&
               Height == other.Height &&
               Center.Equals(other.Center) &&
               GridCellSize == other.GridCellSize &&
               DanmakuRecycleMargin.Equals(other.DanmakuRecycleMargin);
    }

    public override bool Equals(object obj) => obj is BattleAreaConfig other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Width, Height, Center, GridCellSize, DanmakuRecycleMargin);
}

public class BattleSession
{
    public E_Rank rank;
    public E_Character character;
    public E_Weapon weapon;

    public CharacterConfig characterConfig;
    public WeaponConfig weaponConfig;
    public int playerEntityId; // ECS 中的实体 ID
    public int score;
    public int life;
    public int bomb;
}

public class BattleManager : SingletonMono<BattleManager>
{
    World danmakuWorld;

    [SerializeField] Transform playerRoot;
    [SerializeField] BattleArea battleArea;

    public static BattleAreaConfig battleConfig;
    public BattleSession battleSession;
    
    public World DanmakuWorld => danmakuWorld;
    public EntityManager EntityManager => danmakuWorld?.EntityManager;

    protected override void OnSingletonInit()
    {
        EventManager.Instance.RegistEvent(E_Event.BattleStart, StartBattle);
        battleArea.InitBattleArea();
        InitDanmakuWorld();
    }

    public void StartBattle()
    {
        Debug.Log("Battle Start!");
        SceneManager.LoadScene("BattleScene");
        SceneManager.UnloadSceneAsync("TitleScene");
    }

    public void CreatePlayer()
    {

    }

    void InitDanmakuWorld()
    {
        danmakuWorld = new World();
        
        var movementSys = danmakuWorld.AddSystem<MovementSystem>();
        var collisionSys = danmakuWorld.AddSystem<CollisionSystem>();
        collisionSys.Initialize(battleConfig);
        var lifetimeSys = danmakuWorld.AddSystem<LifetimeSystem>();
    }

    #region 核心更新循环（帧同步）
    void FixedUpdate()
    {
        // 使用固定时间步长，确保帧同步
        danmakuWorld?.FixedUpdate(Time.fixedDeltaTime);
    }

    void Update()
    {
        danmakuWorld?.Update(Time.deltaTime);
    }

    void LateUpdate()
    {
        danmakuWorld?.LateUpdate(Time.deltaTime);
    }
    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventManager.Instance.UnRegistEvent(E_Event.BattleStart, StartBattle);     
    }
}
