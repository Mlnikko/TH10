using UnityEngine;

/// <summary>
/// 敌人发射器「朝向玩家」：按最近玩家位置修正 <see cref="DanmakuEmitSystem"/> 的 emitRotRad。
/// </summary>
public static class DanmakuEmitterAimAtPlayerLogic
{
    static bool _simulatedPlayerActive;
    static float _simulatedPlayerX;
    static float _simulatedPlayerY;

    /// <summary>无真实玩家实体时（如关卡时间轴编辑器预览）用该世界坐标作为瞄准目标。</summary>
    public static void SetSimulatedPlayerTarget(float worldX, float worldY)
    {
        _simulatedPlayerActive = true;
        _simulatedPlayerX = worldX;
        _simulatedPlayerY = worldY;
    }

    public static void ClearSimulatedPlayerTarget() => _simulatedPlayerActive = false;

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

        if (!overrideTargetX.HasValue && _simulatedPlayerActive)
        {
            overrideTargetX = _simulatedPlayerX;
            overrideTargetY = _simulatedPlayerY;
        }

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

        int targetIdx = DanmakuHomingLogic.FindNearestTargetIndex(
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
