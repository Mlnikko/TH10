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
/// 逻辑帧之间的表现插值快照（由 <see cref="PresentationPoseSystem"/> 在每逻辑帧末更新）。
/// </summary>
public struct CPresentationPose : IComponent
{
    public float prevX, prevY;
    public float currX, currY;
    public float prevAngleRad, currAngleRad;
    public bool hasSnapshot;
}

/// <summary>
/// 渲染系统使用的标记组件，标记实体需要在当前帧进行表现更新。系统会根据这个组件来决定哪些实体需要同步到GameObject。
/// </summary>
public struct CPoolGetTag : IComponent { }

public struct CPoolRecycleTag : IComponent { }

/// <summary>
/// 跳过「超出弹幕回收区则实体销毁」逻辑（如道中/关底 Boss）；与普通敌人共用 <see cref="CPoolRecycleTag"/> 回收管线以外的豁免。
/// </summary>
public struct CNoOffscreenRecycleTag : IComponent { }

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
    /// <summary>
    /// 绕 Z 轴旋转角，单位为弧度（逻辑层统一弧度；表现层再乘 Mathf.Rad2Deg 得到欧拉角度数）。
    /// </summary>
    public float angleRad;
    public CRotation(float angleRad)
    {
        this.angleRad = angleRad;
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

/// <summary>
/// 掉落物逻辑实体；配置索引指向 <see cref="DropItemConfig"/>。
/// </summary>
public struct CDropItem : IComponent
{
    public int cfgIndex;

    public CDropItem(int cfgIndex)
    {
        this.cfgIndex = cfgIndex;
    }
}

/// <summary>
/// 掉落物竖直上抛运动（每逻辑帧竖直位移：上正下负）；受重力并限制终端下落速度。
/// </summary>
public struct CDropItemMotion : IComponent
{
    public float vyPerFrame;
    public float gravityPerFrame;
    public float maxFallPerFrame;
    /// <summary>上升阶段每逻辑帧自转弧度（由 <see cref="DropItemConfig"/> 烘焙）。</summary>
    public float spinRadPerFrame;
}

/// <summary>
/// 道具吸收线激活后，掉落物被吸引飞向目标(同距取实体索引较小)的吸收区玩家。
/// </summary>
public struct CDropItemMagnet : IComponent
{
    public int targetPlayerEntityIndex;

    public CDropItemMagnet(int targetPlayerEntityIndex)
    {
        this.targetPlayerEntityIndex = targetPlayerEntityIndex;
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
    /// <summary>两次发射之间至少间隔的逻辑帧数；0 表示无间隔（由 <see cref="DanmakuEmitterConfig"/> 在加载时烘焙）。</summary>
    public int launchCooldownFrames;

    public EmitMode emitMode;           // Line, Arc
    public DanmakuSelectMode selectMode; // First, Sequential, Random

    // 弹幕选择器的状态机变量
    public int sequentialIndex;
    public uint randomSeed;

    // ================= 通用参数 (预计算，launchSpeed 为世界单位/逻辑帧) =================
    public float launchSpeed;
    public float emitterPosOffsetX, emitterPosOffsetY;
    /// <summary>发射器旋转偏移（弧度）；由 <see cref="DanmakuEmitterConfig.emitterRotOffsetZ"/>（度）在构造时烘焙。</summary>
    public float emitterRotOffsetRad;
    /// <summary>弹幕生成时的旋转偏移（弧度）；由 <see cref="DanmakuEmitterConfig.danmakuRotOffsetZ"/>（度）在构造时烘焙。</summary>
    public float danmakuRotOffsetRad;

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

        launchCooldownFrames = soConfig.launchCooldownFrames;

        sequentialIndex = 0;
        randomSeed = 0; // 初始化种子，实际使用时需结合全局帧数或实体ID

        // 行为模式
        emitMode = soConfig.emitMode;
        selectMode = soConfig.danmakuSelectMode;
        launchSpeed = soConfig.launchSpeedPerFrame;

        emitterPosOffsetX = soConfig.emitterPosOffset.x;
        emitterPosOffsetY = soConfig.emitterPosOffset.y;
        emitterRotOffsetRad = soConfig.emitterRotOffsetZ * Mathf.Deg2Rad;
        danmakuRotOffsetRad = soConfig.danmakuRotOffsetZ * Mathf.Deg2Rad;
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
    public E_ColliderShape shape;

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

    // // 脏标记
    // public bool isDirty;
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
    public byte characterCfgIndex;   // GameResDB 中 CharacterConfig 的运行时索引（非 E_Character 枚举值）
    public byte weaponCfgIndex;      // GameResDB 中 WeaponConfig 的运行时索引（非 E_Weapon 枚举值）

    /// <summary>通常移速：世界单位 / 逻辑帧（由 <see cref="EntityFactory.CreatePlayer"/> 从配置「单位/秒」换算，逻辑系统不再乘 FrameInterval）。</summary>
    public float moveDistancePerFrame;
    /// <summary>低速移速：世界单位 / 逻辑帧。</summary>
    public float moveSlowDistancePerFrame;

    public float hitRadius;       // 受击判定半径
    public float grazeRadius;     // 擦弹判定半径

    public bool isSlowMode;       // 是否处于慢速模式
    public bool isShooting;       // 是否正在射击
    public bool isBombing;        // 是否正在使用炸弹
    public bool isInvincible;     // 是否无敌

    /// <summary>P 道具 / 火力拾取累计（由 <see cref="DropItemPickupEffects"/> 写入）。</summary>
    public int powerOrbs;
}

#endregion

#region Enemy
public struct CEnemy : IComponent
{
    public int enemyCfgIndex;          // 配置索引, 与敌人配置表对应
    public int currentHealth;            // 当前生命值
}

/// <summary>
/// 由时间轴波次写入：覆盖或追加该敌人的死亡掉落（见 <see cref="E_WaveDropOverrideMode"/>）。
/// </summary>
public struct CEnemyDeathLoot : IComponent
{
    public E_WaveDropOverrideMode waveDropMode;
    /// <summary>烘焙后的 DropItemConfig 索引；Replace/Append 时使用。</summary>
    public int[] waveDropCfgIndices;
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