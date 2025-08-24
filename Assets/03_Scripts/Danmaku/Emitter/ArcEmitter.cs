using UnityEngine;

public class ArcEmitter : DanmakuEmitter
{
    ArcEmitterConfig arcEmitterConfig;

    [Header("环类发射器配置")]
    [SerializeField] int directionCount;
    [SerializeField] float startAngle;
    [SerializeField] float endAngle;
    [SerializeField] bool useRelativeAngle;

    protected override void OnEmitterConfigLoad()
    {
        arcEmitterConfig = (ArcEmitterConfig)emitterConfig;

        if (arcEmitterConfig == null) return;

        directionCount = arcEmitterConfig.DirectionCount;
        startAngle = arcEmitterConfig.StartAngle;
        endAngle = arcEmitterConfig.EndAngle;
        useRelativeAngle = arcEmitterConfig.UseRelativeAngle;
    }

    protected override void OnEmitterConfigSave()
    {
        if (arcEmitterConfig == null) return;

        arcEmitterConfig.DirectionCount = directionCount;
        arcEmitterConfig.StartAngle = startAngle;
        arcEmitterConfig.EndAngle = endAngle;
        arcEmitterConfig.UseRelativeAngle = useRelativeAngle;
    }

    // 计算每个弹幕的角度和方向
    (float angle, Vector2 direction) CalculateBulletDirection(int index)
    {
        // 处理起始和结束角度重合的情况
        float effectiveEndAngle = endAngle;
        if (Mathf.Approximately(startAngle, endAngle))
        {
            effectiveEndAngle = startAngle + 360f;
        }

        float angleRange = effectiveEndAngle - startAngle;
        float angleStep = (directionCount <= 1) ? 0 : angleRange / (directionCount - 1);
        float baseRotation = useRelativeAngle ? transform.eulerAngles.z : 0f;
        float currentAngle = baseRotation + startAngle + (angleStep * index);
        float angleInRadians = currentAngle * Mathf.Deg2Rad;

        Vector2 direction = new(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));

        return (currentAngle, direction);
    }

    protected override void FireDanmaku()
    {
        if (directionCount <= 0) return;

        for (int i = 0; i < directionCount; i++)
        {
            Danmaku danmaku = danmakuPool.Get();
            var transformComp = danmaku.Entity.GetComp<Danmaku_TransformComponent>();

            // 使用共用函数计算角度和方向
            var (angle, direction) = CalculateBulletDirection(i);

            // 设置弹幕位置、旋转和速度
            transformComp.Position = FirePosition;
            transformComp.Rotation = new Vector3(0, 0, angle);
            transformComp.Velocity = direction * startVelocity;

            DanmakuSystem.Instance.AddDanmaku(danmaku);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying && emitterConfig != null)
        {
            Gizmos.color = Color.yellow;

            // 绘制弧线
            int segments = 30;
            Vector3 prevPoint = FirePosition;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                // 使用共用函数计算角度和方向
                var (angle, direction) = CalculateBulletDirectionForVisualization(t);
                Vector3 point = FirePosition + (Vector3)direction * 1f; // 1单位长度

                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, point);
                }

                prevPoint = point;
            }

            // 绘制每个弹幕的发射方向
            if (directionCount > 0)
            {
                Gizmos.color = Color.red;

                for (int i = 0; i < directionCount; i++)
                {
                    // 使用共用函数计算角度和方向
                    var (angle, direction) = CalculateBulletDirection(i);
                    Vector3 endPoint = FirePosition + (Vector3)direction * 1.5f; // 1.5单位长度

                    Gizmos.DrawLine(FirePosition, endPoint);
                }
            }
        }
    }

    // 专门用于可视化的角度计算函数
    (float angle, Vector2 direction) CalculateBulletDirectionForVisualization(float t)
    {
        // 处理起始和结束角度重合的情况
        float effectiveEndAngle = endAngle;
        if (Mathf.Approximately(startAngle, endAngle))
        {
            effectiveEndAngle = startAngle + 360f;
        }
        float angleRange = effectiveEndAngle - startAngle;
        float baseRotation = useRelativeAngle ? transform.eulerAngles.z : 0f;
        float currentAngle = baseRotation + startAngle + (angleRange * t);
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        Vector2 direction = new(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));
        return (currentAngle, direction);
    }
}