using UnityEngine;

public class DanmakuConfig : ScriptableObject , IConfig
{
    [Header("µ¯Ä»Ëõ·Å")]
    public Vector3 LocalScale;

    [Header("µ¯Ä»äÖÈ¾ÉèÖÃ")]
    public Sprite Sprite;
    public Color Color;

    [Header("µ¯Ä»Åö×²Æ÷ÉèÖÃ")]
    public Vector2 ColliderOffset;
    public E_ColliderType ColliderType;
    public Vector2 Size;
    public float Radius;

    [Header("µ¯Ä»ÕóÓª")]
    public E_DanmakuCamp DanmakuCamp;

    [Header("µ¯Ä»ÀàĞÍ")]
    public E_DanmakuType DanmakuType;

    public DanmakuConfig()
    {
        LocalScale = Vector3.one;
        Sprite = null;
        Color = Color.white;
        ColliderOffset = Vector2.zero;
        ColliderType = E_ColliderType.None;
        Size = Vector2.one;
        Radius = 0.5f;
        DanmakuCamp = E_DanmakuCamp.None;
        DanmakuType = E_DanmakuType.Normal;
    }

    public ScriptableObject Load()
    {
        return this;
    }

    public bool Save(ScriptableObject SO)
    {
        throw new System.NotImplementedException();
    }
}
