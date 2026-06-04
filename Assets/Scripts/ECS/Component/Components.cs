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
/// 上一逻辑帧与当前逻辑帧的位置/朝向快照（<see cref="PresentationPoseSystem"/> 维护）。
/// </summary>
public struct CPresentationPose : IComponent
{
    public float prevX, prevY, currX, currY;
    public float prevAngleRad, currAngleRad;
    public bool hasRotation;

    public static CPresentationPose FromPosition(float x, float y, float angleRad, bool withRotation) =>
        new()
        {
            prevX = x,
            currX = x,
            prevY = y,
            currY = y,
            prevAngleRad = angleRad,
            currAngleRad = angleRad,
            hasRotation = withRotation,
        };
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
/// 恒定速度外弧追踪 <see cref="homingTargetLayerMask"/> 内最近实体（见 <see cref="DanmakuHomingLogic"/>）。
/// </summary>
public struct CDanmakuHoming : IComponent
{
    public int targetEnemyIndex;
    public float speedPerFrame;
    public float turnSpeedRadPerFrame;
    public ushort homingTargetLayerMask;
    /// <summary>外弧弯曲侧（+1 / -1）；生成时按目标相对发射器朝向的左右确定。</summary>
    public sbyte curveBendSign;
    /// <summary>1=外弧阶段（绕外侧弯转），0=最短路径追尾。</summary>
    public byte outerArcActive;
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
/// 掉落物出场运动（竖直上抛或定向散射后匀速下落）。
/// </summary>
public struct CDropItemMotion : IComponent
{
    public E_DropMotionMode motionMode;

    /// <summary>竖直上抛：每逻辑帧竖直速度（上正下负）。</summary>
    public float vyPerFrame;
    public float gravityPerFrame;
    public float maxFallPerFrame;
    /// <summary>上升阶段每逻辑帧自转弧度。</summary>
    public float spinRadPerFrame;

    /// <summary>定向散射：0=沿方向减速，1=匀速下落。</summary>
    public byte motionPhase;
    public float burstSpeedPerFrame;
    public float burstDirX;
    public float burstDirY;
    public float burstDecelPerFrame;
    /// <summary>散射结束后的竖直速度（每逻辑帧，负值向下）。</summary>
    public float fallVyPerFrame;
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
    /// <summary>发射器激活逻辑帧；<see cref="DanmakuEmitterActivation.Unset"/> 表示尚未记录。</summary>
    public uint activationFrame;
    /// <summary>两次发射之间至少间隔的逻辑帧数；0 表示无间隔（由 <see cref="DanmakuEmitterConfig"/> 在加载时烘焙）。</summary>
    public int launchCooldownFrames;
    /// <summary>首次齐射前需等待的逻辑帧数（来自 <see cref="DanmakuEmitterConfig.initialLaunchDelaySeconds"/>）。</summary>
    public int initialLaunchDelayFrames;
    /// <summary>最大发射次数；-1 表示无限（来自 <see cref="DanmakuEmitterConfig.launchCount"/>）。</summary>
    public int launchCountMax;
    /// <summary>已完成的发射轮次（一次 Line/Arc 齐射计 1 次）。</summary>
    public int launchCountUsed;

    public EmitMode emitMode;           // Line, Arc, Wave, Grain
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
    /// <summary>每次齐射后 Arc/Wave 起始角递增量（弧度）。</summary>
    public float salvoAngleAdvanceRad;

    public int emitterCamp;
    /// <summary>敌人专用：齐射时朝向最近玩家调整发射角。</summary>
    public bool aimAtPlayer;
    /// <summary>各发射模式在未瞄准时的基准局部角（弧度），用于与玩家方向对齐。</summary>
    public float aimReferenceLocalRad;

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

    // ================= Wave 模式专用 =================
    public float waveCenterAngleRad;
    public float waveSwingRad;
    public float waveOmegaRadPerFrame;
    public float wavePhaseOffsetRad;
    public float waveArcHalfSpreadRad;

    // ================= Grain 模式专用 =================
    public int grainBulletCount;
    public float grainBaseAngleRad;
    public float grainConeHalfRad;
    public float grainSpeedMin;
    public float grainSpeedMax;
    public float grainSpawnScatterRadius;

    // 指向 SO 中的索引数组
    public int[] danmakuCfgIndices;

    // ================= 构造函数：负责“烘焙”逻辑 =================
    public CDanmakuEmitter(DanmakuEmitterConfig soConfig)
    {
        isEmitting = false;
        lastFireFrame = 0;
        activationFrame = DanmakuEmitterSalvoInfo.ActivationFrameUnset;

        launchCooldownFrames = soConfig.launchCooldownFrames;
        initialLaunchDelayFrames = soConfig.initialLaunchDelayFrames;
        launchCountMax = DanmakuEmitterSalvoInfo.NormalizeLaunchCountMax(soConfig.launchCount);
        launchCountUsed = 0;

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
        salvoAngleAdvanceRad = soConfig.salvoAngleAdvanceRad;
        emitterCamp = (int)soConfig.emitterCamp;
        aimAtPlayer = soConfig.aimAtPlayer && soConfig.emitterCamp == EmitterCamp.Enemy;
        aimReferenceLocalRad = ComputeAimReferenceLocalRad(soConfig);

        // --- Line 模式烘焙 ---
        lineCount = Mathf.Max(1, soConfig.lineModeConfig.lineCount);
        lineSpacingHalf = soConfig.lineModeConfig.lineSpacing * 0.5f; // 预计算常数

        // 预计算单位向量和垂直向量 (原代码逻辑: offsetX = ... * dirY, offsetY = ... * -dirX)
        Vector2 dir = soConfig.lineModeConfig.lineDirection.normalized;
        lineDirUnitX = dir.x;
        lineDirUnitY = dir.y;
        lineDirPerpX = -dir.y; // 垂直向量 X
        lineDirPerpY = dir.x;  // 垂直向量 Y

        // --- Arc / Wave 共用弧几何（Wave 在发射时动态偏移中心角）---
        var arcGeom = ComputeArcGeometry(soConfig);
        arcBulletCount = arcGeom.bulletCount;
        arcRadius = arcGeom.radius;
        arcStartAngleRad = arcGeom.startAngleRad;
        arcAngleStepRad = arcGeom.angleStepRad;
        arcDirectionSign = arcGeom.directionSign;
        waveArcHalfSpreadRad = arcGeom.halfSpreadRad;

        // --- Wave 模式烘焙 ---
        var wave = soConfig.waveModeConfig;
        waveCenterAngleRad = wave.centerAngleDeg * Mathf.Deg2Rad;
        waveSwingRad = wave.swingDegrees * Mathf.Deg2Rad;
        waveOmegaRadPerFrame = soConfig.waveOmegaRadPerFrame;
        wavePhaseOffsetRad = wave.phaseOffsetDeg * Mathf.Deg2Rad;

        // --- Grain 模式烘焙 ---
        var grain = soConfig.grainModeConfig;
        grainBulletCount = Mathf.Max(1, grain.bulletCount);
        grainBaseAngleRad = grain.baseAngleDeg * Mathf.Deg2Rad;
        grainConeHalfRad = grain.coneHalfAngleDeg * Mathf.Deg2Rad;
        grainSpeedMin = soConfig.launchSpeedPerFrame * grain.ResolveSpeedMinScale();
        grainSpeedMax = soConfig.launchSpeedPerFrame * grain.ResolveSpeedMaxScale();
        grainSpawnScatterRadius = grain.spawnScatterRadius;

        // 资源引用
        danmakuCfgIndices = soConfig.danmakuCfgIndices ?? Array.Empty<int>();
    }

    static float ComputeAimReferenceLocalRad(DanmakuEmitterConfig soConfig)
    {
        switch (soConfig.emitMode)
        {
            case EmitMode.Line:
            {
                Vector2 dir = soConfig.lineModeConfig.lineDirection.normalized;
                return Mathf.Atan2(dir.y, dir.x);
            }
            case EmitMode.Arc:
            {
                var arc = soConfig.arcModeConfig;
                int count = Mathf.Max(1, arc.arcBulletCount);
                float stepRad = count > 1
                    ? arc.arcAngle * Mathf.Deg2Rad / (count - 1)
                    : 0f;
                int dirSign = arc.arcClockwise ? 1 : -1;
                return arc.arcStartAngle * Mathf.Deg2Rad + stepRad * dirSign * ((count - 1) * 0.5f);
            }
            case EmitMode.Wave:
                return soConfig.waveModeConfig.centerAngleDeg * Mathf.Deg2Rad;
            case EmitMode.Grain:
                return soConfig.grainModeConfig.baseAngleDeg * Mathf.Deg2Rad;
            default:
                return 0f;
        }
    }

    static (
        int bulletCount,
        float radius,
        float startAngleRad,
        float angleStepRad,
        int directionSign,
        float halfSpreadRad) ComputeArcGeometry(DanmakuEmitterConfig soConfig)
    {
        ArcModeConfig arc = soConfig.emitMode == EmitMode.Wave
            ? WaveToArc(soConfig.waveModeConfig)
            : soConfig.arcModeConfig;

        int bulletCount = Mathf.Max(1, arc.arcBulletCount);
        float totalRad = arc.arcAngle * Mathf.Deg2Rad;
        float stepRad = bulletCount > 1
            ? totalRad / (bulletCount - 1)
            : 0f;

        return (
            bulletCount,
            arc.arcRadius,
            arc.arcStartAngle * Mathf.Deg2Rad,
            stepRad,
            arc.arcClockwise ? 1 : -1,
            totalRad * 0.5f);
    }

    static ArcModeConfig WaveToArc(WaveModeConfig wave) => new()
    {
        arcStartAngle = 0f,
        arcAngle = wave.spreadAngleDeg,
        arcRadius = wave.arcRadius,
        arcBulletCount = wave.bulletCount,
        arcClockwise = wave.clockwise,
    };
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

    /// <summary>移动碰撞盒（<see cref="CharacterConfig.moveColliderConfig"/>）烘焙，用于战斗区边缘限制。</summary>
    public byte moveColliderShape;
    public float moveColliderOffsetX;
    public float moveColliderOffsetY;
    public float moveColliderRadius;
    public float moveColliderHalfW;
    public float moveColliderHalfH;

    public bool isSlowMode;       // 是否处于慢速模式
    public bool isShooting;       // 是否正在射击
    public bool isBombing;        // 是否正在使用炸弹
    public bool isInvincible;     // 是否无敌

    /// <summary>受击复活后的无敌剩余逻辑帧；>0 时忽略伤害并闪动表现。</summary>
    public int invincibleFramesRemaining;

    /// <summary>P 道具 / 火力拾取累计（由 <see cref="DropItemPickupEffects"/> 写入）。</summary>
    public int powerOrbs;

    /// <summary>主发射器子实体索引；无发射器时为 -1。</summary>
    public int primaryEmitterEntityIndex;

    /// <summary>武器发射布局变体：0 通常 / 1 低速（主炮配置切换 + 槽位收束）。</summary>
    public byte emitterSlotLayoutVariant;

    /// <summary>副炮槽位收束插值（0=展开，1=完全收束）；由 <see cref="PlayerControlSystem"/> 每逻辑帧推进。</summary>
    public float secondarySlotConvergeT;

    /// <summary>已应用的副炮 Power 档 <see cref="WeaponPowerSecondaryLayout.minPowerOrbs"/>；<see cref="int.MinValue"/> 表示尚未同步。</summary>
    public int appliedSecondaryPowerMinOrbs;

    /// <summary>低速模式下已应用的主炮 Power 档 <see cref="WeaponPowerPrimarySlowLayout.minPowerOrbs"/>。</summary>
    public int appliedPrimarySlowPowerMinOrbs;
}

/// <summary>玩家最近若干逻辑帧的位置轨迹，用于需要沿玩家历史路径移动的武器副炮。</summary>
public struct CPlayerMotionTrail : IComponent
{
    public float[] xs;
    public float[] ys;
    public int head;
    public int count;

    public bool IsValid => xs != null && ys != null && xs.Length == ys.Length && xs.Length > 0;
    public int Capacity => IsValid ? xs.Length : 0;

    public static CPlayerMotionTrail Create(int capacity, float initialX, float initialY)
    {
        capacity = Mathf.Max(1, capacity);
        var trail = new CPlayerMotionTrail
        {
            xs = new float[capacity],
            ys = new float[capacity],
            head = 0,
            count = 1,
        };

        for (int i = 0; i < capacity; i++)
        {
            trail.xs[i] = initialX;
            trail.ys[i] = initialY;
        }

        return trail;
    }

    public void Record(float x, float y)
    {
        if (!IsValid)
            return;

        head = (head + 1) % xs.Length;
        xs[head] = x;
        ys[head] = y;
        if (count < xs.Length)
            count++;
    }

    public bool TrySampleFramesAgo(int framesAgo, out float x, out float y)
    {
        x = y = 0f;
        if (!IsValid || count <= 0)
            return false;

        framesAgo = Mathf.Clamp(framesAgo, 0, count - 1);
        int index = head - framesAgo;
        if (index < 0)
            index += xs.Length;

        x = xs[index];
        y = ys[index];
        return true;
    }
}

/// <summary>受击摧毁后等待复活的会话状态（无表现层）。</summary>
public struct CPlayerRespawnPending : IComponent
{
    public byte playerIndex;
    public byte characterCfgIndex;
    public byte weaponCfgIndex;
    public int remainingHealth;
    public int invincibleFramesAfterSpawn;
    public int framesUntilSpawn;
    public float spawnX;
    public float spawnY;
}

/// <summary>挂在敌人/Boss 弹幕发射子实体上，跟随宿主位姿。</summary>
public struct CEnemyEmitterOwnership : IComponent
{
    public int ownerEnemyEntityIndex;
}

/// <summary>挂在玩家武器发射子实体上，用于同步位置与射击状态。</summary>
public struct CPlayerEmitterOwnership : IComponent
{
    public int ownerPlayerEntityIndex;
    public E_WeaponEmitterSlotRole role;
    public byte secondarySlotIndex;
    public int emitterCfgIndex;
    public float slotOffsetX;
    public float slotOffsetY;

    /// <summary><see cref="DanmakuEmitterConfig.emitterPosOffset"/>（不含武器槽位偏移）。</summary>
    public float emitterBaseOffsetX;
    public float emitterBaseOffsetY;
}

#endregion

#region Enemy
public struct CEnemy : IComponent
{
    public int enemyCfgIndex;          // 配置索引, 与敌人配置表对应
    public int currentHealth;            // 当前生命值
    /// <summary>烘焙自 <see cref="EnemyConfig.enemyType"/>，逻辑帧内只读。</summary>
    public byte enemyType;
}

/// <summary>
/// 由时间轴波次写入：覆盖或追加该敌人的死亡掉落（见 <see cref="E_WaveDropOverrideMode"/>）。
/// </summary>
public struct CEnemyDeathLoot : IComponent
{
    public E_WaveDropOverrideMode waveDropMode;
    /// <summary>烘焙后的掉落条目；Replace/Append 时使用。</summary>
    public BakedDeathDropEntry[] waveDrops;
}

public enum E_MidBossPhase : byte
{
    Entry = 0,
    OnField = 1,
    Exit = 2,
    Done = 3,
}

/// <summary>中场 Boss 遭遇阶段与路径烘焙索引（由 <see cref="MidBossEncounterSystem"/> 驱动）。</summary>
public struct CMidBossEncounter : IComponent
{
    public E_MidBossPhase phase;
    public uint phaseStartFrame;
    /// <summary>逻辑帧：到达后进入退场（入场结束帧 + 在场时长）。</summary>
    public uint onFieldEndFrame;
    public int encounterCfgIndex;
    public int entryRouteBakeIndex;
    public int loopRouteBakeIndex;
    public int exitRouteBakeIndex;
    public int entryDurationFrames;
    public int exitDurationFrames;
    /// <summary>场内循环路径起点（入场结束或登场点），退场路径以此为原点。</summary>
    public float loopOriginX;
    public float loopOriginY;
}

/// <summary>关底 Boss 登场/场内路径（由 <see cref="MainBossEncounterSystem"/> 驱动）。</summary>
public struct CMainBossEncounter : IComponent
{
    public E_MainBossPathPhase pathPhase;
    public int encounterCfgIndex;
    public int entryRouteBakeIndex;
    public int loopRouteBakeIndex;
    public int entryDurationFrames;
}

public enum E_MainBossPathPhase : byte
{
    Entry = 0,
    Loop = 1,
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
