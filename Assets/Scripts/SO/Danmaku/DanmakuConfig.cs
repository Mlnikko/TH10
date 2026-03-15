using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDanmakuConfig", menuName = "Configs/Danmaku")]
public class DanmakuConfig : GameConfig , IReferenceResolver
{
    [Header("弹幕预制体")]
    public string danmakuPrefabId;
    [NonSerialized]
    public int danmakuPrefabIndex;

    public int poolSize = 100;

    [Header("弹幕类型")]
    public E_DanmakuType danmakuType = E_DanmakuType.Normal;

    [Header("弹幕Transform")]
    public float scale = 1f;

    [Header("弹幕渲染设置")]
    public Sprite sprite = null;
    public Color color = Color.white;

    [Header("弹幕碰撞器设置")]
    public ColliderConfig colliderConfig = new();

    [Header("弹幕伤害")]
    public float damage = 1f;

    [Header("弹幕追踪设置")]
    [HideInInspector] public float HomingTurnSpeed = 5f;
    [HideInInspector] public LayerMask HomingTargetLayers = 1; // 例如 Player 层

    [Header("弹幕运动设置")]
    public bool IsAccelerating = false;
    [HideInInspector] public float MaxSpeed = 10f;
    [HideInInspector] public float Acceleration = 2f;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(danmakuPrefabId))
            danmakuPrefabId = danmakuPrefabId.ToLowerInvariant().Trim();
    }
#endif

    public void ResolveReferences(GameResDB resDb)
    {
        danmakuPrefabIndex = resDb.GetPrefabIndex(danmakuPrefabId);
        if (danmakuPrefabIndex == -1)
        {
            Logger.Warn(
                $"[DanmakuConfig] Prefab not found: '{danmakuPrefabId}' " +
                $"(configId: {configId})",
                LogTag.Resource
            );
        }
    }
}
