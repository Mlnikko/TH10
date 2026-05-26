using System;

/// <summary>
/// 逻辑帧末尾记录位置/朝向快照，供 <see cref="PresentationMotion"/> 在无速度或曲线运动实体上插值。
/// </summary>
public class PresentationPoseSystem : BaseSystem
{
    public override void OnLogicTick(uint currentframe)
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

            pose.prevX = pose.currX;
            pose.prevY = pose.currY;
            pose.currX = pos.x;
            pose.currY = pos.y;

            if (!EntityManager.HasComponent<CRotation>(idx))
            {
                pose.hasRotation = false;
                continue;
            }

            ref readonly var rot = ref rotations[idx];
            pose.hasRotation = true;
            pose.prevAngleRad = pose.currAngleRad;
            pose.currAngleRad = rot.angleRad;
        }
    }
}
