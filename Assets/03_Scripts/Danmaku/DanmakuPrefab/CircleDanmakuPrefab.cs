using UnityEngine;
public class CircleDanmakuPrefab : DanmakuPrefab
{
    CircleDanmakuConfig circleDanmakuConfig;
    [SerializeField] float radius;
    protected override void OnDanmakuConfigLoad()
    {
        circleDanmakuConfig = (CircleDanmakuConfig)danmakuConfig;

        if (circleDanmakuConfig == null) return;

        radius = circleDanmakuConfig.Radius;
    }

    protected override void OnDanmakuConfigSave()
    {
        if (circleDanmakuConfig == null) return;

        circleDanmakuConfig.Radius = radius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(ColliderCenter, radius);
    }
}
