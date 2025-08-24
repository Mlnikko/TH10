
using UnityEngine;

[CreateAssetMenu(fileName = "NewCircleDanmakuConfig", menuName = "Danmaku/CircleDanmakuConfig")]
public class CircleDanmakuConfig : DanmakuConfig
{
    [Header("‘≤–ŒµØƒª…Ë÷√")]
    public float Radius;

    public CircleDanmakuConfig() : base()
    {
        ColliderType = E_ColliderType.Circle;
        Radius = 0.1f;
    }
}
