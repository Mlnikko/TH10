/// <summary>
/// 实体组件掩码：每位对应一种 <see cref="IComponent"/>。
/// 新增组件类型时须分配新位，并更新 <see cref="GetMask{T}"/> 与 <see cref="EntityManager"/> 的 <c>ClearComponentsFromMask</c>。
/// </summary>
public static class ComponentMaskBits
{
    public const uint CPoolRecycleTag         = 1u << 0;
    public const uint CPoolGetTag             = 1u << 1;
    public const uint CNoOffscreenRecycleTag  = 1u << 2;
    public const uint CGameObjectLink         = 1u << 3;
    public const uint CPresentationPose       = 1u << 4;

    public const uint CPlayerEmitterOwnership = 1u << 5;
    public const uint CDanmakuEmitter         = 1u << 6;
    public const uint CDanmaku                = 1u << 7;
    public const uint CDropItemMagnet         = 1u << 8;
    public const uint CDropItemMotion         = 1u << 9;
    public const uint CDropItem               = 1u << 10;
    public const uint CEnemyDeathLoot         = 1u << 11;
    public const uint CEnemyPathMovement      = 1u << 12;
    public const uint CEnemy                  = 1u << 13;
    public const uint CPlayer                 = 1u << 14;
    public const uint CHealth                 = 1u << 15;
    public const uint CCollider               = 1u << 16;
    public const uint CVelocity               = 1u << 17;
    public const uint CRotation               = 1u << 18;
    public const uint CPosition               = 1u << 19;
    public const uint CStageState             = 1u << 20;
    public const uint CDanmakuBezierHoming    = 1u << 21;
    public const uint CMidBossEncounter       = 1u << 22;

    public static uint GetMask<T>() where T : struct, IComponent
    {
        if (typeof(T) == typeof(CPoolRecycleTag)) return CPoolRecycleTag;
        if (typeof(T) == typeof(CPoolGetTag)) return CPoolGetTag;
        if (typeof(T) == typeof(CNoOffscreenRecycleTag)) return CNoOffscreenRecycleTag;
        if (typeof(T) == typeof(CGameObjectLink)) return CGameObjectLink;
        if (typeof(T) == typeof(CPresentationPose)) return CPresentationPose;

        if (typeof(T) == typeof(CPlayerEmitterOwnership)) return CPlayerEmitterOwnership;
        if (typeof(T) == typeof(CDanmakuEmitter)) return CDanmakuEmitter;
        if (typeof(T) == typeof(CDanmaku)) return CDanmaku;
        if (typeof(T) == typeof(CDropItemMagnet)) return CDropItemMagnet;
        if (typeof(T) == typeof(CDropItemMotion)) return CDropItemMotion;
        if (typeof(T) == typeof(CDropItem)) return CDropItem;
        if (typeof(T) == typeof(CEnemyDeathLoot)) return CEnemyDeathLoot;
        if (typeof(T) == typeof(CEnemyPathMovement)) return CEnemyPathMovement;
        if (typeof(T) == typeof(CEnemy)) return CEnemy;
        if (typeof(T) == typeof(CPlayer)) return CPlayer;
        if (typeof(T) == typeof(CHealth)) return CHealth;
        if (typeof(T) == typeof(CCollider)) return CCollider;
        if (typeof(T) == typeof(CVelocity)) return CVelocity;
        if (typeof(T) == typeof(CRotation)) return CRotation;
        if (typeof(T) == typeof(CPosition)) return CPosition;
        if (typeof(T) == typeof(CStageState)) return CStageState;
        if (typeof(T) == typeof(CDanmakuBezierHoming)) return CDanmakuBezierHoming;
        if (typeof(T) == typeof(CMidBossEncounter)) return CMidBossEncounter;

        return 0;
    }
}
