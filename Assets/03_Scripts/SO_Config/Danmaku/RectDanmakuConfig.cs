using UnityEngine;

[CreateAssetMenu(fileName = "NewDanmakuConfig", menuName = "Danmaku/RectDanmakuConfig")]
public class RectDanmakuConfig : DanmakuConfig
{
    [Header("¾ØÐÎµ¯Ä»ÉèÖÃ")]
    public Vector2 Size;

    public RectDanmakuConfig() : base()
    {
        ColliderType = E_ColliderType.Rect;
        Size = Vector2.one;
    }
}
