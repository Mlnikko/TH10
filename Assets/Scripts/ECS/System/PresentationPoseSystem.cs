using System;

/// <summary>
/// 每逻辑帧结束后记录表现插值用的 prev/curr 姿态（须注册在战斗管线最末）。
/// </summary>
public class PresentationPoseSystem : BaseSystem
{
    public override void OnLogicTick(uint currentFrame)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CPresentationPose>();
        if (indices.Length == 0)
            return;

        var poses = EntityManager.GetComponentSpan<CPresentationPose>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            ref var pose = ref poses[idx];
            ref readonly var pos = ref positions[idx];
            float angle = idx < rotations.Length ? rotations[idx].angleRad : 0f;

            if (!pose.hasSnapshot)
            {
                pose.prevX = pose.currX = pos.x;
                pose.prevY = pose.currY = pos.y;
                pose.prevAngleRad = pose.currAngleRad = angle;
                pose.hasSnapshot = true;
                continue;
            }

            pose.prevX = pose.currX;
            pose.prevY = pose.currY;
            pose.prevAngleRad = pose.currAngleRad;
            pose.currX = pos.x;
            pose.currY = pos.y;
            pose.currAngleRad = angle;
        }
    }
}
