using System;

/// <summary>
/// Components为实体附加的数据结构，用于存储实体的各种属性和状态。
/// 必须为值类型（struct），以提高性能和内存效率。
/// </summary>

public interface IComponent { }

public struct CGameObjectLink : IComponent
{
    public int gid; // 全局唯一表现 ID
    public int prefabIndex; // 预制体索引（用于对象池）

    public CGameObjectLink(int prefabIndex)
    {
        this.gid = 0;
        this.prefabIndex = prefabIndex;
    }
}

public struct CPosition : IComponent
{
    public float x, y;
    public CPosition(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}

public struct CVelocity : IComponent
{
    public float vx, vy;
    public CVelocity(float vx, float vy)
    {
        this.vx = vx;
        this.vy = vy;
    }
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

    public CDanmaku(int ownerId, int cfgIndex)
    {
        this.ownerId = ownerId;
        this.cfgIndex = cfgIndex;
    }
}

#endregion

#region 弹幕发射器组件
public struct CDanmakuEmitter : IComponent
{
    public bool isEnabled;
    public int cfgIndex;
    public uint lastFireFrame;

    public CDanmakuEmitter(bool isEnabled, int cfgIndex)
    {
        this.isEnabled = isEnabled;
        this.cfgIndex = cfgIndex;
        this.lastFireFrame = 0;
    }
}

//public struct CDanmakuEmitterRunTime : IComponent
//{
   
//}

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

    public CPlayer(byte playerIndex, byte characterId, byte weaponId)
    {
        this.playerIndex = playerIndex;
        this.characterId = characterId;
        this.weaponId = weaponId;
    }
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
