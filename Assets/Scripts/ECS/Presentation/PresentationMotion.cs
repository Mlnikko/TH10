using UnityEngine;

/// <summary>
/// 将逻辑帧状态采样为渲染帧显示坐标（插值 / 本地预测）。
/// </summary>
public static class PresentationMotion
{
    const float VelocityEpsilonSq = 1e-8f;

    public static bool TrySampleDisplayTransform(
        in EntityManager em,
        Entity entity,
        out float x,
        out float y,
        out float angleRad)
    {
        if (!em.IsValid(entity) || !em.HasComponent<CPosition>(entity))
        {
            x = y = angleRad = 0f;
            return false;
        }

        int idx = entity.Index;
        ref readonly var pos = ref em.GetComponentSpan<CPosition>()[idx];

        if (!PresentationRuntime.SmoothingEnabled)
            return TrySampleLogicSnap(em, idx, pos, out x, out y, out angleRad);

        float alpha = PresentationRuntime.LogicFrameAlpha;

        if (PresentationRuntime.IsLogicStalled
            && em.HasComponent<CPlayer>(idx)
            && TrySampleLocalPlayerPrediction(em, entity, alpha, out x, out y))
        {
            angleRad = SampleDisplayAngle(em, idx, alpha);
            return true;
        }

        if (em.HasComponent<CVelocity>(idx))
        {
            ref readonly var vel = ref em.GetComponentSpan<CVelocity>()[idx];
            if (vel.vx * vel.vx + vel.vy * vel.vy > VelocityEpsilonSq)
            {
                float back = 1f - alpha;
                x = pos.x - vel.vx * back;
                y = pos.y - vel.vy * back;
                angleRad = SampleDisplayAngle(em, idx, alpha);
                return true;
            }
        }

        if (em.HasComponent<CPresentationPose>(idx))
        {
            ref readonly var pose = ref em.GetComponentSpan<CPresentationPose>()[idx];
            x = Mathf.Lerp(pose.prevX, pose.currX, alpha);
            y = Mathf.Lerp(pose.prevY, pose.currY, alpha);
            angleRad = pose.hasRotation
                ? LerpAngleRad(pose.prevAngleRad, pose.currAngleRad, alpha)
                : SampleDisplayAngle(em, idx, alpha);
            return true;
        }

        return TrySampleLogicSnap(em, idx, pos, out x, out y, out angleRad);
    }

    static bool TrySampleLogicSnap(
        in EntityManager em,
        int idx,
        CPosition pos,
        out float x,
        out float y,
        out float angleRad)
    {
        x = pos.x;
        y = pos.y;
        angleRad = em.HasComponent<CRotation>(idx)
            ? em.GetComponentSpan<CRotation>()[idx].angleRad
            : 0f;
        return true;
    }

    static float SampleDisplayAngle(in EntityManager em, int idx, float alpha)
    {
        if (em.HasComponent<CPresentationPose>(idx))
        {
            ref readonly var pose = ref em.GetComponentSpan<CPresentationPose>()[idx];
            if (pose.hasRotation)
                return LerpAngleRad(pose.prevAngleRad, pose.currAngleRad, alpha);
        }

        return em.HasComponent<CRotation>(idx)
            ? em.GetComponentSpan<CRotation>()[idx].angleRad
            : 0f;
    }

    static bool TrySampleLocalPlayerPrediction(
        in EntityManager em,
        Entity entity,
        float alpha,
        out float x,
        out float y)
    {
        x = y = 0f;

        ref readonly var player = ref em.GetComponentSpan<CPlayer>()[entity.Index];
        if (player.playerIndex != RoomManager.LocalPlayerIndex)
            return false;

        var battle = BattleManager.Instance;
        if (battle == null || battle.ActiveBattleWorld == null)
            return false;

        uint frame = battle.ActiveBattleWorld.LogicFrameTimer.CurrentFrame;
        var input = InputManager.Instance.SampleLocalInput(player.playerIndex, frame);

        float distancePerFrame = input.SlowMode
            ? player.moveSlowDistancePerFrame
            : player.moveDistancePerFrame;
        float dx = input.MoveHorizontal * distancePerFrame;
        float dy = input.MoveVertical * distancePerFrame;

        float t = Mathf.Min(alpha, 1f);
        ref readonly var pos = ref em.GetComponentSpan<CPosition>()[entity.Index];
        x = pos.x + dx * t;
        y = pos.y + dy * t;
        return true;
    }

    static float LerpAngleRad(float fromRad, float toRad, float t)
    {
        float fromDeg = fromRad * Mathf.Rad2Deg;
        float toDeg = toRad * Mathf.Rad2Deg;
        return Mathf.LerpAngle(fromDeg, toDeg, t) * Mathf.Deg2Rad;
    }
}
