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

/// <summary>
/// 掉落物数据配置（碰撞体、表现预制体、竖直上抛运动与拾取效果）。
/// </summary>
[CreateAssetMenu(fileName = "NewDropItemConfig", menuName = "Configs/DropItem/DropItemConfig")]
public class DropItemConfig : GameConfig, IReferenceResolver, ILogicTimingBake
{
    [Header("表现")]
    public string pickupPrefabId;
    [NonSerialized] public int pickupPrefabIndex = -1;

    [Tooltip("可选：拾取后写入 SpriteRenderer.sprite（预制体需含 SpriteRenderer）")]
    public Sprite pickupSprite;

    [Header("竖直上抛运动")]
    [Tooltip("初速度（世界单位/秒，向上为正）")]
    public float initialUpSpeed = 2.2f;

    [Tooltip("下落重力加速度（世界单位/秒²，取正值）")]
    public float fallGravity = 14f;

    [Tooltip("最大下落速度（世界单位/秒，取正值）；达到后匀速下落")]
    public float maxFallSpeed = 2f;

    [NonSerialized] public float initialUpPerFrame;
    [NonSerialized] public float gravityPerFrame;
    [NonSerialized] public float maxFallPerFrame;

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
    }

#if UNITY_EDITOR
    void Reset()
    {
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
