using UnityEngine;

[CreateAssetMenu(fileName = "NewDanmakuConfig", menuName = "Configs/Danmaku")]
public class DanmakuConfig : GameConfig
{
    [Header("µ¯Ä»ÀàÐÍ")]
    public DanmakuType DanmakuType = DanmakuType.Normal;

    [Header("µ¯Ä»Ëõ·Å")]
    public Vector2 LocalScale = Vector2.one;

    [Header("µ¯Ä»äÖÈ¾ÉèÖÃ")]
    public Sprite Sprite = null;
    public Color Color = Color.white;

    [Header("µ¯Ä»Åö×²Æ÷ÉèÖÃ")]
    public E_ColliderType ColliderType = E_ColliderType.None;
    public E_ColliderLayer ColliderLayer = E_ColliderLayer.Default;
    public Vector2 ColliderOffset = Vector2.zero;
    public Vector2 Size = Vector2.zero;
    public float Radius = 0;  

    [Header("µ¯Ä»ÉËº¦")]
    public float Damage = 1f;

    [Header("µ¯Ä»×·×ÙÉèÖÃ")]
    [HideInInspector] public float HomingTurnSpeed = 5f;
    [HideInInspector] public LayerMask HomingTargetLayers = 1; // ÀýÈç Player ²ã

    [Header("µ¯Ä»ÔË¶¯ÉèÖÃ")]
    public bool IsAccelerating = false;
    [HideInInspector] public float MaxSpeed = 10f;
    [HideInInspector] public float Acceleration = 2f;

    public override string AddressableKeyPrefix => ConfigHelper.DANMAKU_CONFIG_PREFIX;
}
