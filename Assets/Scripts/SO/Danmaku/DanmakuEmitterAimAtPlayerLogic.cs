using UnityEngine;

/// <summary>
/// 敌人发射器「朝向玩家」：按最近玩家位置修正 <see cref="DanmakuEmitSystem"/> 的 emitRotRad。
/// </summary>
public static class DanmakuEmitterAimAtPlayerLogic
{
    public static float ResolveEmitRotRad(
        in CDanmakuEmitter emitter,
        float emitPosX,
        float emitPosY,
        float entityRotRad,
        EntityManager em,
        float? overrideTargetX = null,
        float? overrideTargetY = null)
    {
        float baseRotRad = entityRotRad + emitter.emitterRotOffsetRad;
        if (!emitter.aimAtPlayer)
            return baseRotRad;

        if (!TryResolveTargetPosition(
                emitPosX, emitPosY, em, overrideTargetX, overrideTargetY, out float targetX, out float targetY))
            return baseRotRad;

        float aimAngleRad = Mathf.Atan2(targetY - emitPosY, targetX - emitPosX);
        return aimAngleRad - emitter.aimReferenceLocalRad + emitter.emitterRotOffsetRad;
    }

    static bool TryResolveTargetPosition(
        float emitPosX,
        float emitPosY,
        EntityManager em,
        float? overrideTargetX,
        float? overrideTargetY,
        out float targetX,
        out float targetY)
    {
        if (overrideTargetX.HasValue && overrideTargetY.HasValue)
        {
            targetX = overrideTargetX.Value;
            targetY = overrideTargetY.Value;
            return true;
        }

        if (em == null)
        {
            targetX = 0f;
            targetY = 0f;
            return false;
        }

        int targetIdx = DanmakuBezierHomingLogic.FindNearestTargetIndex(
            em, emitPosX, emitPosY, (ushort)E_ColliderLayer.Player);
        if (targetIdx < 0)
        {
            targetX = 0f;
            targetY = 0f;
            return false;
        }

        ref readonly var targetPos = ref em.GetComponentSpan<CPosition>()[targetIdx];
        targetX = targetPos.x;
        targetY = targetPos.y;
        return true;
    }
}
