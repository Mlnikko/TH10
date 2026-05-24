using UnityEngine;

/// <summary>
/// 表现层平滑：在「当前逻辑帧姿态」基础上按上一逻辑帧位移（CVelocity）做帧内外推。
/// 本地玩家仅在锁步等待远程输入时额外用实时按键外推，避免与权威位置频繁回拉。
/// </summary>
public static class PresentationMotion
{
    const float VelocityEpsilon = 0.0001f;
    const float MaxStallPredictionFrames = 2f;

    public static bool TrySampleDisplayTransform(
        in EntityManager em,
        Entity entity,
        LogicFrameDriver driver,
        bool logicStalled,
        byte localPlayerIndex,
        out float x,
        out float y,
        out float angleRad)
    {
        ref readonly var pos = ref em.GetComponent<CPosition>(entity);
        x = pos.x;
        y = pos.y;
        angleRad = 0f;
        bool hasRotation = em.HasComponent<CRotation>(entity);
        if (hasRotation)
            angleRad = em.GetComponent<CRotation>(entity).angleRad;

        float alpha = driver.GetRenderAlpha();
        float overshoot = logicStalled ? driver.GetOvershootAlpha() : 0f;

        if (TrySampleVelocityMotion(em, entity, alpha, ref x, ref y, ref angleRad, hasRotation))
            return true;

        if (em.HasComponent<CPlayer>(entity))
        {
            ref readonly var player = ref em.GetComponent<CPlayer>(entity);
            if (player.playerIndex == localPlayerIndex
                && TrySampleLocalPlayerStallPrediction(
                    in player, driver, logicStalled, overshoot, localPlayerIndex,
                    ref x, ref y, ref angleRad, hasRotation, em, entity))
                return true;
        }

        if (TrySamplePoseMotion(em, entity, alpha, ref x, ref y, ref angleRad))
            return true;

        return true;
    }

    /// <summary>锁步等待远程输入时，在权威位置上按当前按键少量外推（不叠加帧内 α，避免超前回拉）。</summary>
    static bool TrySampleLocalPlayerStallPrediction(
        in CPlayer player,
        LogicFrameDriver driver,
        bool logicStalled,
        float overshoot,
        byte localPlayerIndex,
        ref float x,
        ref float y,
        ref float angleRad,
        bool hasRotation,
        in EntityManager em,
        Entity entity)
    {
        if (!logicStalled || overshoot <= 0f)
            return false;

        var input = InputManager.Instance.SampleLocalInput(localPlayerIndex, driver.CurrentFrame);
        float dist = input.SlowMode ? player.moveSlowDistancePerFrame : player.moveDistancePerFrame;
        float frameVx = input.MoveHorizontal * dist;
        float frameVy = input.MoveVertical * dist;

        float lead = Mathf.Min(overshoot, MaxStallPredictionFrames);
        x += frameVx * lead;
        y += frameVy * lead;

        if (GlobalBattleData.IsInitialized)
        {
            x = Mathf.Clamp(x, GlobalBattleData.AreaData.Left, GlobalBattleData.AreaData.Right);
            y = Mathf.Clamp(y, GlobalBattleData.AreaData.Bottom, GlobalBattleData.AreaData.Top);
        }

        if (frameVx * frameVx + frameVy * frameVy > VelocityEpsilon * VelocityEpsilon)
            angleRad = Mathf.Atan2(frameVy, frameVx);
        else if (hasRotation)
            angleRad = em.GetComponent<CRotation>(entity).angleRad;

        return true;
    }

    /// <summary>pos + velocity×α：帧内向前平滑，α=0 时与逻辑坐标一致。</summary>
    static bool TrySampleVelocityMotion(
        in EntityManager em,
        Entity entity,
        float alpha,
        ref float x,
        ref float y,
        ref float angleRad,
        bool hasRotation)
    {
        if (!em.HasComponent<CVelocity>(entity))
            return false;

        ref readonly var vel = ref em.GetComponent<CVelocity>(entity);
        if (vel.vx * vel.vx + vel.vy * vel.vy < VelocityEpsilon * VelocityEpsilon)
            return false;

        ref readonly var pos = ref em.GetComponent<CPosition>(entity);
        x = pos.x + vel.vx * alpha;
        y = pos.y + vel.vy * alpha;

        if (em.HasComponent<CEnemy>(entity))
            angleRad = 0f;
        else if (hasRotation)
            angleRad = Mathf.Atan2(vel.vy, vel.vx);

        return true;
    }

    /// <summary>无 CVelocity 时用快照位移（如掉落物）做帧内外推。</summary>
    static bool TrySamplePoseMotion(
        in EntityManager em,
        Entity entity,
        float alpha,
        ref float x,
        ref float y,
        ref float angleRad)
    {
        if (!em.HasComponent<CPresentationPose>(entity))
            return false;

        ref readonly var pose = ref em.GetComponent<CPresentationPose>(entity);
        if (!pose.hasSnapshot)
            return false;

        float dx = pose.currX - pose.prevX;
        float dy = pose.currY - pose.prevY;

        ref readonly var pos = ref em.GetComponent<CPosition>(entity);
        x = pos.x + dx * alpha;
        y = pos.y + dy * alpha;

        // 掉落物自转由 CRotation 驱动；敌人仅平移，不随位移方向旋转。
        // 掉落物被磁吸时由 CPosition 驱动；保持角度为 0，不用位移方向旋转。
        if (em.HasComponent<CDropItem>(entity) && em.HasComponent<CDropItemMagnet>(entity))
        {
            angleRad = 0f;
            return dx * dx + dy * dy >= VelocityEpsilon * VelocityEpsilon;
        }

        if (em.HasComponent<CDropItem>(entity))
        {
            float prevDeg = pose.prevAngleRad * Mathf.Rad2Deg;
            float currDeg = pose.currAngleRad * Mathf.Rad2Deg;
            angleRad = Mathf.LerpAngle(prevDeg, currDeg, alpha) * Mathf.Deg2Rad;
            return true;
        }

        if (em.HasComponent<CEnemy>(entity))
        {
            angleRad = 0f;
            return dx * dx + dy * dy >= VelocityEpsilon * VelocityEpsilon;
        }

        if (dx * dx + dy * dy < VelocityEpsilon * VelocityEpsilon)
            return false;

        if (Mathf.Abs(dx) + Mathf.Abs(dy) > VelocityEpsilon)
            angleRad = Mathf.Atan2(dy, dx);
        else
        {
            float prevDeg = pose.prevAngleRad * Mathf.Rad2Deg;
            float currDeg = pose.currAngleRad * Mathf.Rad2Deg;
            angleRad = Mathf.LerpAngle(prevDeg, currDeg, alpha) * Mathf.Deg2Rad;
        }

        return true;
    }

    public static void InitializePoseFromEntity(EntityManager em, Entity entity)
    {
        ref var pose = ref em.GetComponent<CPresentationPose>(entity);
        ref readonly var pos = ref em.GetComponent<CPosition>(entity);
        float angle = 0f;
        if (em.HasComponent<CRotation>(entity))
            angle = em.GetComponent<CRotation>(entity).angleRad;

        pose.prevX = pose.currX = pos.x;
        pose.prevY = pose.currY = pos.y;
        pose.prevAngleRad = pose.currAngleRad = angle;
        pose.hasSnapshot = true;
    }
}
