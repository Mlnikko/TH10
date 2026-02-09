using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDanmakuConfig", menuName = "Configs/Danmaku")]
public class DanmakuConfig : GameConfig , IReferenceResolver
{
    [Header("µ¯Ä»Ô¤ÖÆÌå")]
    public string danmakuPrefabId;
    [NonSerialized]
    public int danmakuPrefabIndex;

    [Header("µ¯Ä»ÀàĞÍ")]
    public DanmakuType danmakuType = DanmakuType.Normal;

    [Header("µ¯Ä»Ëõ·Å")]
    public Vector2 localScale = Vector2.one;

    [Header("µ¯Ä»äÖÈ¾ÉèÖÃ")]
    public Sprite sprite = null;
    public Color color = Color.white;

    [Header("µ¯Ä»Åö×²Æ÷ÉèÖÃ")]
    public E_ColliderType colliderType = E_ColliderType.None;
    public E_ColliderLayer colliderLayer = E_ColliderLayer.Default;
    public Vector2 colliderOffset = Vector2.zero;
    public Vector2 size = Vector2.zero;
    public float radius = 0;  

    [Header("µ¯Ä»ÉËº¦")]
    public float damage = 1f;

    [Header("µ¯Ä»×·×ÙÉèÖÃ")]
    [HideInInspector] public float HomingTurnSpeed = 5f;
    [HideInInspector] public LayerMask HomingTargetLayers = 1; // ÀıÈç Player ²ã

    [Header("µ¯Ä»ÔË¶¯ÉèÖÃ")]
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
