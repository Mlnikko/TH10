
using System;


/// <summary>
/// Components为实体附加的数据结构，用于存储实体的各种属性和状态。
/// 必须为值类型（struct），以提高性能和内存效率。
/// </summary>

public interface IComponent { }

// 所有需要同步到 GameObject 的实体都带这个组件
public struct CPresentationLink : IComponent
{
    public int presentationId; // 全局唯一表现 ID
}

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
public enum E_ColliderType : byte
{
    None,
    Rect,
    Circle,
}

[Flags]
public enum E_ColliderLayer : ushort
{
    None = 0,

    Default = 1 << 0,

    Player = 1 << 1,
    Enemy = 1 << 2,
    PlayerDanmaku = 1 << 3,
    EnemyDanmaku = 1 << 4,
    Item = 1 << 5,
}

public struct CCollider : IComponent
{
    // 是否激活
    public bool active;

    // 碰撞体类型
    public E_ColliderType type;

    // 碰撞层
    public E_ColliderLayer layer;

    // 碰撞掩码
    public E_ColliderLayer mask;

    // 相对偏移
    public float offsetX;
    public float offsetY;

    // 脏标记
    public bool dirty;

    // Circle
    public float radius;

    // Rect
    public float width;
    public float height;
}
#endregion

#region PlayerComponent
public struct CPlayer : IComponent
{
    public byte playerIndex;   // 玩家ID
    public byte characterId;   // 角色ID, 与角色配置表对应
    public byte weaponId;      // 武器ID, 与武器配置表对应
}

// 玩家属性
public struct CPlayerAttribute : IComponent
{
    public float moveSpeed;       // 移动速度
    public float moveSlowSpeed;   // 慢速移动速度
    public float hitRadius;       // 受击判定半径
    public float grazeRadius;     // 擦弹判定半径
}

public struct CPlayerRunTime : IComponent
{
    public bool isSlowMode;       // 是否处于慢速模式
    public bool isShooting;       // 是否正在射击
    public bool isInvincible;     // 是否无敌
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
