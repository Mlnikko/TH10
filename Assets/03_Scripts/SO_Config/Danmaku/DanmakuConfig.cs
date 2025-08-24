using UnityEngine;
public enum E_ColliderType
{
    None,
    Rect,
    Circle,   
}

public class DanmakuConfig : ScriptableObject
{
    [Header("µ¯Ä»Ëõ·Å")]
    public Vector3 LocalScale;

    [Header("µ¯Ä»äÖÈ¾ÉèÖÃ")]
    public Sprite Sprite;
    public Color Color;

    [Header("µ¯Ä»Åö×²Æ÷ÉèÖÃ")]
    public Vector2 ColliderOffset;
    public E_ColliderType ColliderType;

    public DanmakuConfig()
    {
        LocalScale = Vector3.one;
        Sprite = null;
        Color = Color.white;
        ColliderOffset = Vector2.zero;
        ColliderType = E_ColliderType.None;
    }
}
