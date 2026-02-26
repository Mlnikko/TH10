using System;
using UnityEngine;

/// <summary>
/// Components为实体附加的数据结构，用于存储实体的各种属性和状态。
/// 必须为值类型（struct），以提高性能和内存效率。
/// </summary>

public interface IComponent { }


/// <summary>
/// 表现层GO同步组件，负责将ECS实体与Unity的GameObject进行关联，并通过Updater驱动表现更新。
/// </summary>
public struct CGameObjectLink : IComponent
{
    public GameObject GameObject;
    public IGameObjectUpdater Updater;
    public bool IsDirty; // 标记是否需要同步
}

public enum E_PresentationState : byte
{
    None = 0,       // 无操作
    Spawn = 1,      // 需要创建 GameObject
    Despawn = 2,    // 需要销毁/回收 GameObject
    Update = 3      // (可选) 仅需要同步位置/旋转，不创建/销毁
}

/// <summary>
/// 渲染系统使用的标记组件，标记实体需要在当前帧进行表现更新。系统会根据这个组件来决定哪些实体需要同步到GameObject。
/// </summary>
public struct CPoolGet : IComponent { }

public struct CPoolRecycle : IComponent { }

#region Base

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

#endregion

#region 弹幕组件
public enum DanmakuType
    {
        Normal,
        Homing
    }

public struct CDanmaku : IComponent
{
    public int cfgIndex; // 弹幕配置索引

    public CDanmaku(int cfgIndex)
    {
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

#endregion

#region Collider
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

#region Player
public struct CPlayer : IComponent
{
    public byte playerIndex;   // 玩家ID
    public byte characterCfgIndex;   // 角色ID, 与角色配置表对应
    public byte weaponCfgIndex;      // 武器ID, 与武器配置表对应

    public CPlayer(byte playerIndex, byte characterCfgIndex, byte weaponCfgIndex)
    {
        this.playerIndex = playerIndex;
        this.characterCfgIndex = characterCfgIndex;
        this.weaponCfgIndex = weaponCfgIndex;
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

#region Enemy
public struct CEnemy : IComponent
{
    public ushort enemyId;       // 敌人ID
    public float hp;              // 生命值
    public float maxHp;           // 最大生命值
    public float speed;           // 移动速度
    public float hitRadius;       // 受击判定半径
}
#endregion