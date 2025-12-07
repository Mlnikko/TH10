using UnityEngine;

[CreateAssetMenu(fileName = "NewHomingDanmakuConfig", menuName = "DanmakuConfiger/HomingDanmakuConfig")]
public class HomingDanmakuConfig : DanmakuConfig
{
    [Header("×·×Ùµ¯Ä»ÉèÖÃ")]
    public float HomingTurnSpeed;

    public HomingDanmakuConfig() : base()
    {
        HomingTurnSpeed = 5f;
    }
}
