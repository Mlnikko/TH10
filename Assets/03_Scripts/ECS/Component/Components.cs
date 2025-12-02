/// <summary>
/// Components为实体附加的数据结构，用于存储实体的各种属性和状态。
/// 必须为值类型（struct），以提高性能和内存效率。
/// </summary>

public interface IComponent { }

public struct CPosition : IComponent
{
    public float x, y;
    public CPosition(float x, float y) => (this.x, this.y) = (x, y);
}

public struct CVelocity : IComponent
{
    public float vx, vy;
    public CVelocity(float vx, float vy) => (this.vx, this.vy) = (vx, vy);
}

public struct CLifetime : IComponent
{
    public float ttl; // time to live，单位：秒
    public CLifetime(float seconds) => ttl = seconds;
}

#region DanmakuComponent
public enum DanmakuType
{
    Normal,
    Homing
}

public struct CDanmaku : IComponent
{
    public DanmakuType type; // 枚举：Bullet, Enemy, Player, Effect...
    public ushort ownerId;  // 谁发射的（用于伤害归属）
    public float damage;     // 伤害值
}
#endregion

#region ColliderComponent
public enum E_ColliderType
{
    None,
    Rect,
    Circle,
}

public enum E_ColliderLayer
{
    Default = 0,
    Player = 1,
    Enemy = 2,
    PlayerDanmaku = 3,
    EnemyDanmaku = 4,
    Item = 5,
}

public struct CCollider : IComponent
{
    public bool Active;
    public E_ColliderType Type;

    // 共享字段：偏移（相对于实体位置）
    public float OffsetX;
    public float OffsetY;

    // Circle 专用
    public float Radius;

    // Rect 专用
    public float Width;
    public float Height;

    // 碰撞层（用于过滤）
    public byte Layer;           // 0=Player, 1=Enemy, 2=DanmakuConfiger, 3=PlayerBullet...
    public byte Mask;            // 与哪些层碰撞（位掩码）

    // 标记：是否需要重建网格索引
    public bool Dirty;
}
#endregion

#region PlayerComponent
public struct CPlayer : IComponent
{
    public byte playerId;      // 玩家ID（用于多人游戏）
    public CharacterConfig characterConfig; // 玩家角色配置数据
    public float speed;           // 移动速度
    public float slowSpeed;       // 慢速移动速度
    public float hitRadius;       // 受击判定半径
    public float grazeRadius;     // 擦弹判定半径
    public bool isSlowMode;       // 是否处于慢速模式
}
#endregion

#region EnemyComponent
public struct CEnemy : IComponent
{
    public ushort enemyId;       // 敌人ID
    public float hp;              // 生命值
    public float maxHp;           // 最大生命值
    public float speed;           // 移动速度
    public float hitRadius;       // 受击判定半径
}
#endregion
