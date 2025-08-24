using UnityEngine;
public class RectDanmakuPrefab : DanmakuPrefab
{
    RectDanmakuConfig rectDanmakuconfig;
    [SerializeField] Vector2 size;

    protected override void OnDanmakuConfigLoad()
    {
        rectDanmakuconfig = (RectDanmakuConfig)danmakuConfig;

        if (rectDanmakuconfig == null) return;

        size = rectDanmakuconfig.Size;
    }

    protected override void OnDanmakuConfigSave()
    {
        if(rectDanmakuconfig == null) return;

        rectDanmakuconfig.Size = size;
    } 

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(ColliderCenter, size);
    }
}
