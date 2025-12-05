using UnityEngine;

public class DanmakuConfig : GameConfig
{
    [Header("µ¯Ä»Ëõ·Å")]
    public Vector2 LocalScale;

    [Header("µ¯Ä»äÖÈ¾ÉèÖÃ")]
    public Sprite Sprite;
    public Color Color;

    [Header("µ¯Ä»Åö×²Æ÷ÉèÖÃ")]
    public E_ColliderType ColliderType;
    public E_ColliderLayer ColliderLayer;
    public Vector2 ColliderOffset; 
    public Vector2 Size;
    public float Radius;

    [Header("µ¯Ä»ÀàĞÍ")]
    public DanmakuType DanmakuType;

    [Header("µ¯Ä»ÉËº¦")]
    public float Damage;

    public DanmakuConfig()
    {
        LocalScale = Vector3.one;
        Sprite = null;
        Color = Color.white;
        ColliderOffset = Vector2.zero;
        ColliderType = E_ColliderType.None;
        Size = Vector2.zero;
        Radius = 0;
        DanmakuType = DanmakuType.Normal;
        Damage = 1f;
    }
}
