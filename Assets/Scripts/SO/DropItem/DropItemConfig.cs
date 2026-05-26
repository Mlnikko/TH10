using System;
using UnityEngine;

/// <summary>
/// 掉落物种类：拾取后在确定性逻辑内套用对应效果（见 <see cref="DropItemPickupEffects"/>）。
/// </summary>
public enum E_DropKind : byte
{
    None = 0,
    Score = 1,
    Heal = 2,
    Power = 3,
}

/// <summary>掉落物出场运动模式。</summary>
public enum E_DropMotionMode : byte
{
    /// <summary>竖直上抛，受重力后终端匀速下落（原逻辑）。</summary>
    VerticalToss = 0,
    /// <summary>沿指定方向减速至停止，再匀速竖直下落。</summary>
    DirectionalBurstThenFall = 1,
}

/// <summary>
/// 掉落物数据配置（碰撞体、表现预制体、出场运动与拾取效果）。
/// </summary>
[CreateAssetMenu(fileName = "NewDropItemConfig", menuName = "Configs/DropItem/DropItemConfig")]
public class DropItemConfig : GameConfig, IReferenceResolver, ILogicTimingBake
{
    [Header("表现")]
    [Tooltip("池化预制体 archetype Id（小写）；与 ConfigId 无关。多条 DropItemConfig 可共用，表现见 pickupSprite。见 DropItemPrefabArchetypes。")]
    [PoolPrefabId(E_PoolCategory.Drop)]
    public string pickupPrefabId;
    [NonSerialized] public int pickupPrefabIndex = -1;

    [Tooltip("可选：拾取后写入 SpriteRenderer.sprite（预制体需含 SpriteRenderer）")]
    public Sprite pickupSprite;

    [Header("出场运动")]
    public E_DropMotionMode motionMode = E_DropMotionMode.VerticalToss;

    [Header("竖直上抛（motionMode = VerticalToss）")]
    [Tooltip("初速度（世界单位/秒，向上为正）")]
    public float initialUpSpeed = 2.2f;

    [Tooltip("下落重力加速度（世界单位/秒²，取正值）")]
    public float fallGravity = 14f;

    [Tooltip("最大下落速度（世界单位/秒，取正值）；达到后匀速下落")]
    public float maxFallSpeed = 2f;

    [Tooltip("上升阶段自转速度（度/秒，顺时针为正）；到最高点后归零并停止旋转")]
    public float riseSpinDegreesPerSecond = 360f;

    [Header("定向散射后下落（motionMode = DirectionalBurstThenFall）")]
    [Tooltip("散射初速度（世界单位/秒）")]
    public float burstInitialSpeed = 2.5f;

    [Tooltip("散射方向（世界坐标，运行时归一化）")]
    public Vector2 burstDirection = new Vector2(0.35f, 1f);

    [Tooltip("沿散射方向的减速加速度（世界单位/秒²，取正值）")]
    public float burstDeceleration = 8f;

    [Tooltip("散射停止后的匀速下落速度（世界单位/秒，取正值，向下）")]
    public float fallSpeedAfterBurst = 2f;

    [NonSerialized] public float initialUpPerFrame;
    [NonSerialized] public float gravityPerFrame;
    [NonSerialized] public float maxFallPerFrame;
    [NonSerialized] public float spinRadPerFrame;

    [NonSerialized] public float burstInitialPerFrame;
    [NonSerialized] public float burstDecelPerFrame;
    [NonSerialized] public float burstDirX;
    [NonSerialized] public float burstDirY;
    [NonSerialized] public float fallVyAfterBurstPerFrame;

    [Header("碰撞（通常为 Item 层，掩码含 Player）")]
    public ColliderConfig colliderConfig;

    [Header("拾取效果")]
    public E_DropKind dropKind = E_DropKind.Score;

    [Tooltip("Score：加分；Heal：回复生命值；Power：增加火力计数（CPlayer.powerOrbs）")]
    public int effectAmount = 100;

    public void ResolveReferences(GameResDB resDb)
    {
        pickupPrefabIndex = resDb.GetPrefabIndex(pickupPrefabId);
        if (pickupPrefabIndex < 0)
        {
            Logger.Warn(
                $"[DropItemConfig] Prefab not found: '{pickupPrefabId}' (configId: {ConfigId})",
                LogTag.Resource);
        }
    }

    public void BakeLogicTiming(uint logicFPS)
    {
        if (logicFPS <= 0) logicFPS = 60;
        float invFps = 1f / logicFPS;

        initialUpPerFrame = Mathf.Max(initialUpSpeed, 0f) * invFps;
        gravityPerFrame = fallGravity * invFps;
        maxFallPerFrame = Mathf.Max(maxFallSpeed, 0f) * invFps;
        spinRadPerFrame = riseSpinDegreesPerSecond * Mathf.Deg2Rad * invFps;

        burstInitialPerFrame = Mathf.Max(burstInitialSpeed, 0f) * invFps;
        burstDecelPerFrame = Mathf.Max(burstDeceleration, 0f) * invFps;
        fallVyAfterBurstPerFrame = -Mathf.Max(fallSpeedAfterBurst, 0f) * invFps;

        Vector2 dir = burstDirection;
        if (dir.sqrMagnitude < 1e-8f)
            dir = Vector2.up;
        dir.Normalize();
        burstDirX = dir.x;
        burstDirY = dir.y;
    }

#if UNITY_EDITOR
    void Reset()
    {
        pickupPrefabId = DropItemPrefabArchetypes.Pickup;
        colliderConfig = new ColliderConfig
        {
            shape = E_ColliderShape.Circle,
            layer = E_ColliderLayer.Item,
            mask = E_ColliderLayer.Player,
            radius = 0.06f
        };
    }

    void OnValidate()
    {
        if (!string.IsNullOrEmpty(pickupPrefabId))
            pickupPrefabId = pickupPrefabId.ToLowerInvariantTrimmed();
    }
#endif
}
