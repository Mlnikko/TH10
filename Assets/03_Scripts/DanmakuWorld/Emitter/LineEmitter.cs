using UnityEngine;

public sealed class LineEmitter : DanmakuEmitter
{
    LineEmitterConfig lineEmitterConfig;

    [Header("œﬂ¿‡∑¢…‰∆˜≈‰÷√")]

    [Range(-1, 1)]
    [SerializeField] float dirX;

    [Range(-1, 1)]
    [SerializeField] float dirY;

    Vector2 Direction
    {
        get
        {
            return new(dirX, dirY);
        }
        set
        {
            dirX = value.x;
            dirY = value.y;
        }
    }

    [SerializeField] int lineCount;
    [SerializeField] float lineSpace;   

    protected override void OnEmitterConfigLoad()
    {
        lineEmitterConfig = (LineEmitterConfig)emitterConfig;
        if (lineEmitterConfig == null) return;

        emitterType = E_EmitterType.Line;
        Direction = lineEmitterConfig.Direction;
        lineCount = lineEmitterConfig.LineCount;
        lineSpace = lineEmitterConfig.LineSpace;
    }

    protected override void OnEmitterConfigSave()
    {
        if (lineEmitterConfig == null) return;

        lineEmitterConfig.EmitterType = emitterType;
        lineEmitterConfig.Direction = Direction;
        lineEmitterConfig.LineCount = lineCount;
        lineEmitterConfig.LineSpace = lineSpace;

        emitterConfig = (LineEmitterConfig)emitterConfig;
    }

    protected override void OnDanmakuFire()
    {
        base.OnDanmakuFire();

        for (int i = 0; i < lineCount; i++)
        {
            Danmaku danmaku = danmakuPool.Get();
            Vector2 offset = CalculateLineOffset(i);
            danmaku.Position = FirePosition + (Vector3)offset;
            
            float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

            danmaku.Rotation = new Vector3(0, 0, angle);
            danmaku.Velocity = Direction.normalized * launchSpeed;
            DanmakuSystem.Instance.AddDanmaku(danmaku);
        }
    }

    Vector2 CalculateLineOffset(int lineIndex)
    {
        if (lineCount <= 1) return Vector2.zero;
        float startOffset = -(lineCount - 1) * lineSpace / 2f;
        Vector2 perpendicular = new Vector2(-Direction.y, Direction.x).normalized;
        return perpendicular * (startOffset + lineIndex * lineSpace);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            if (lineCount > 0)
            {
                Gizmos.color = Color.yellow;

                for (int i = 0; i < lineCount; i++)
                {
                    Vector2 offset = CalculateLineOffset(i);
                    Vector3 linePosition = FirePosition + (Vector3)offset;

                    Gizmos.DrawSphere(linePosition, 0.05f);
                    Gizmos.DrawLine(linePosition, linePosition + (Vector3)Direction.normalized * 0.5f);
                }

                if (lineCount > 1)
                {
                    Vector2 firstOffset = CalculateLineOffset(0);
                    Vector2 lastOffset = CalculateLineOffset(lineCount - 1);

                    Vector3 firstPos = FirePosition + (Vector3)firstOffset;
                    Vector3 lastPos = FirePosition + (Vector3)lastOffset;

                    Gizmos.DrawLine(firstPos, lastPos);
                }
            }
        }

    }
}
