using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDanmakuConfig", menuName = "Configs/Danmaku")]
public class DanmakuConfig : GameConfig, IReferenceResolver, ILogicTimingBake
{
    [Header("弹幕预制体")]
    [Tooltip("池化预制体 archetype Id（小写），与 ConfigId 无关；多条弹幕 Config 可共用同一 prefab，表现由下方 sprite/scale 驱动。见 DanmakuPrefabArchetypes。")]
    public string danmakuPrefabId;
    [NonSerialized]
    public int danmakuPrefabIndex;

    [Header("弹幕类型")]
    public E_DanmakuType danmakuType = E_DanmakuType.Normal;

    [Header("弹幕Transform")]
    public float scale = 1f;

    [Header("弹幕渲染设置")]
    public Sprite sprite = null;

    [Header("弹幕碰撞器设置")]
    public ColliderConfig colliderConfig = new();

    [Header("弹幕伤害")]
    public float damage = 1f;

    [Header("命中表现")]
    [Tooltip("弹幕回收时在回收位置播放的纯粒子特效（Effect 池）；留空则不播放")]
    [PoolPrefabId(E_PoolCategory.Effect)]
    public string hitEffectPrefabId;

    [NonSerialized] public int hitEffectPrefabIndex = -1;

    [Header("追踪弹幕（Bezier 路径）")]
    [Tooltip("追踪目标所在碰撞层；玩家弹通常选 Enemy")]
    public E_ColliderLayer homingTargetLayers = E_ColliderLayer.Enemy;

    [Tooltip("沿一条 Bezier 逼近目标所需时间（秒）；越小转弯越急")]
    [Min(0.02f)]
    public float homingBezierDurationSeconds = 0.35f;

    [Tooltip("曲线弯曲强度（0=直线，约 0.2~0.6 为常见弧弯）")]
    [Range(0f, 1.5f)]
    public float homingCurveStrength = 0.35f;

    [Header("弹幕运动设置")]
    public bool IsAccelerating = false;
    [HideInInspector] public float MaxSpeed = 10f;
    [HideInInspector] public float Acceleration = 2f;

    [NonSerialized] public float homingProgressPerFrame;
    [NonSerialized] public ushort homingTargetLayerMask;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(danmakuPrefabId))
            danmakuPrefabId = StringHelper.NormalizeResourceId(danmakuPrefabId);

        if (!string.IsNullOrEmpty(hitEffectPrefabId))
            hitEffectPrefabId = StringHelper.NormalizeResourceId(hitEffectPrefabId);

        if (homingBezierDurationSeconds < 0.02f)
            homingBezierDurationSeconds = 0.02f;
    }
#endif

    public void ResolveReferences(GameResDB resDb)
    {
        danmakuPrefabIndex = resDb.GetPrefabIndex(danmakuPrefabId);
        if (danmakuPrefabIndex == -1)
        {
            Logger.Warn(
                $"[DanmakuConfig] Prefab not found: '{danmakuPrefabId}' " +
                $"(configId: {ConfigId})",
                LogTag.Resource
            );
        }

        homingTargetLayerMask = (ushort)homingTargetLayers;

        hitEffectPrefabIndex = resDb.GetPrefabIndex(hitEffectPrefabId);
        if (!string.IsNullOrEmpty(hitEffectPrefabId) && hitEffectPrefabIndex < 0)
        {
            Logger.Warn(
                $"[DanmakuConfig] Hit effect prefab not found: '{hitEffectPrefabId}' (configId: {ConfigId})",
                LogTag.Resource);
        }
    }

    public void BakeLogicTiming(uint logicFPS)
    {
        if (logicFPS <= 0)
            logicFPS = 60;

        int frames = Mathf.Max(1, Mathf.CeilToInt(homingBezierDurationSeconds * logicFPS));
        homingProgressPerFrame = 1f / frames;
    }
}
