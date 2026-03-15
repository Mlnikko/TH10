using System;
using System.Runtime.InteropServices;
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
    public IGameObjectUpdater Updater;
    public bool IsDirty; // 标记是否需要同步
}

/// <summary>
/// 渲染系统使用的标记组件，标记实体需要在当前帧进行表现更新。系统会根据这个组件来决定哪些实体需要同步到GameObject。
/// </summary>
public struct CPoolGetTag : IComponent { }

public struct CPoolRecycleTag : IComponent { }

#region 基础组件

public struct CPosition : IComponent
{
    public float x, y;
    public CPosition(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}

public struct CRotation : IComponent
{
    public float rotZ;
    public CRotation(float rotZ)
    {
        this.rotZ = rotZ;
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
public enum E_DanmakuType { Normal, Homing }

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
[StructLayout(LayoutKind.Sequential)]
public struct CDanmakuEmitter : IComponent
{
    // ================= 动态状态 (每帧变化) =================
    public bool isEmitting;
    public uint lastFireFrame;
    public float launchInterval;

    public EmitMode emitMode;           // Line, Arc
    public DanmakuSelectMode selectMode; // First, Sequential, Random

    // 弹幕选择器的状态机变量
    public int sequentialIndex;
    public uint randomSeed;

    // ================= 通用参数 (预计算) =================
    public float launchSpeed;
    public float emitterPosOffsetX, emitterPosOffsetY;
    public float emitterRotOffsetZ;

    public float danmakuRotOffsetZ;

    public int emitterCamp;

    // ================= Line 模式专用 (预计算向量) =================
    public float lineDirUnitX, lineDirUnitY;
    public float lineDirPerpX, lineDirPerpY; // 垂直向量分量
    public int lineCount;
    public float lineSpacingHalf;       // 预计算 spacing * 0.5 或其他常数因子

    // ================= Arc 模式专用 (预计算三角函数) =================
    public int arcBulletCount;
    public float arcRadius;
    public float arcStartAngleRad;      // 起始角度 (弧度)
    public float arcAngleStepRad;       // 预计算: (arcAngle / (count-1)) * Deg2Rad
    public int arcDirectionSign;        // 1 或 -1，替代 clockwise 布尔判断

    // 指向 SO 中的索引数组
    public int[] danmakuCfgIndices;

    // ================= 构造函数：负责“烘焙”逻辑 =================
    public CDanmakuEmitter(DanmakuEmitterConfig soConfig)
    {
        isEmitting = false;
        lastFireFrame = 0;

        launchInterval = soConfig.launchInterval;

        sequentialIndex = 0;
        randomSeed = 0; // 初始化种子，实际使用时需结合全局帧数或实体ID

        // 行为模式
        emitMode = soConfig.emitMode;
        selectMode = soConfig.danmakuSelectMode;
        launchSpeed = soConfig.launchSpeed;

        emitterPosOffsetX = soConfig.emitterPosOffset.x;
        emitterPosOffsetY = soConfig.emitterPosOffset.y;
        emitterRotOffsetZ = soConfig.emitterRotOffsetZ;

        danmakuRotOffsetZ = soConfig.danmakuRotOffsetZ;
        emitterCamp = (int)soConfig.emitterCamp;

        // --- Line 模式烘焙 ---
        lineCount = soConfig.lineModeConfig.lineCount;
        lineSpacingHalf = soConfig.lineModeConfig.lineSpacing * 0.5f; // 预计算常数

        // 预计算单位向量和垂直向量 (原代码逻辑: offsetX = ... * dirY, offsetY = ... * -dirX)
        Vector2 dir = soConfig.lineModeConfig.lineDirection.normalized;
        lineDirUnitX = dir.x;
        lineDirUnitY = dir.y;
        lineDirPerpX = -dir.y; // 垂直向量 X
        lineDirPerpY = dir.x;  // 垂直向量 Y

        // --- Arc 模式烘焙 ---
        arcBulletCount = soConfig.arcModeConfig.arcBulletCount;
        arcRadius = soConfig.arcModeConfig.arcRadius;
        arcDirectionSign = soConfig.arcModeConfig.arcClockwise ? 1 : -1;

        // 角度转弧度，并预计算步长 (避免循环内除法)
        float totalRad = soConfig.arcModeConfig.arcAngle * Mathf.Deg2Rad;
        arcStartAngleRad = soConfig.arcModeConfig.arcStartAngle * Mathf.Deg2Rad;
        if (soConfig.arcModeConfig.arcBulletCount > 1)
            arcAngleStepRad = totalRad / (soConfig.arcModeConfig.arcBulletCount - 1);
        else
            arcAngleStepRad = 0f;

        // 资源引用
        danmakuCfgIndices = soConfig.danmakuCfgIndices ?? Array.Empty<int>();
    }
}

#endregion

#region Collider

public enum E_ColliderShape : byte { None, Rect, Circle }

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
    public bool isActive;

    // 碰撞体类型
    public E_ColliderShape type;

    // 碰撞层
    public E_ColliderLayer layer;

    // 碰撞掩码
    public E_ColliderLayer mask;

    // 相对偏移
    public float offsetX, offsetY;

    // Circle
    public float radius;

    // Rect
    public float width, height;

    // 脏标记
    public bool isDirty;

    public CCollider(bool isActive, E_ColliderShape type, E_ColliderLayer layer, E_ColliderLayer mask, float offsetX, float offsetY, float radius, float width, float height)
    {
        this.isActive = isActive;
        this.type = type;
        this.layer = layer;
        this.mask = mask;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        this.radius = radius;
        this.width = width;
        this.height = height;
        isDirty = false; // 默认未修改
    }
}
#endregion

#region Health
public struct CHealth : IComponent
{
    public int currentHealth;
    public int maxHealth;

    public CHealth(int currentHealth, int maxHealth)
    {
        this.currentHealth = currentHealth;
        this.maxHealth = maxHealth;
    }
}
#endregion

#region Player
public struct CPlayer : IComponent
{
    public byte playerIndex;   // 玩家ID
    public byte characterCfgIndex;   // 角色ID, 与角色配置表对应
    public byte weaponCfgIndex;      // 武器ID, 与武器配置表对应

    public float moveSpeed;
    public float moveSlowSpeed;

    public float hitRadius;       // 受击判定半径
    public float grazeRadius;     // 擦弹判定半径

    public bool isSlowMode;       // 是否处于慢速模式
    public bool isShooting;       // 是否正在射击
    public bool isBombing;        // 是否正在使用炸弹
    public bool isInvincible;     // 是否无敌
}

#endregion

#region Enemy
public struct CEnemy : IComponent
{
    public int enemyCfgIndex;          // 配置索引, 与敌人配置表对应
    public int currentHealth;            // 当前生命值
}
#endregion


public enum E_StageState { None, MidStage, BossIntro, BossFight, BossDefeated, StageClear }
public struct CStageState : IComponent
{
    public E_StageState currentState;
    public uint stateEnterFrame; // 进入当前状态的帧数，用于计算持续时间
    public int currentBossPhaseIndex; // 当前BOSS阶段索引
    public Entity bossEntity; // 当前活跃BOSS的Entity ID
}