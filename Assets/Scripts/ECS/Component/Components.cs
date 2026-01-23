using System;

/// <summary>
/// Components为实体附加的数据结构，用于存储实体的各种属性和状态。
/// 必须为值类型（struct），以提高性能和内存效率。
/// </summary>

public interface IComponent { }

// 所有需要同步到 GameObject 的实体都带这个组件
public struct CGameObjectLink : IComponent
{
    public int gameObjectId; // 全局唯一表现 ID
}

public struct CPosition : IComponent
{
    public float x, y;
}

public struct CVelocity : IComponent
{
    public float vx, vy;
}

public struct CLifetime : IComponent
{
    public uint spawnFrame;      // 实体创建时的逻辑帧号
    public uint maxLifeFrames;   // 最大生存帧数
}

#region 弹幕组件
public enum DanmakuType
{
    Normal,
    Homing
}

public struct CDanmaku : IComponent
{
    public int ownerId;  // 谁发射的
    public int cfgIndex; // 弹幕配置索引
}

public struct CDanmakuRuntime : IComponent
{
    public float speed;
    public float homingAngle; // 仅 Homing 弹幕使用，当前追踪角度
}
#endregion

#region 弹幕发射器组件
public struct CDanmakuEmitter : IComponent
{
    public int cfgIndex;
}

public struct CDanmakuEmitterRunTime : IComponent
{
    public bool isEnabled;
    public uint lastFireFrame;
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
    public float offsetX, offsetY;

    // 脏标记
    public bool dirty;

    // Circle
    public float radius;

    // Rect
    public float width, height;
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
    public float moveSpeedPerFrame;      // e.g. 0.05f （= 3.0 / 60）
    public float moveSlowSpeedPerFrame;  // e.g. 0.025f

    public float hitRadius;       // 受击判定半径
    public float grazeRadius;     // 擦弹判定半径
}

public struct CPlayerRunTime : IComponent
{
    public bool isSlowMode;       // 是否处于慢速模式
    public bool isShooting;       // 是否正在射击
    public bool isBombing;        // 是否正在使用炸弹
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
