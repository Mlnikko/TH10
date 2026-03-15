using UnityEngine;

public static class GizmosDrawer
{
    /// <summary>
    /// 绘制碰撞体 Gizmos (支持均匀缩放 float scale)
    /// </summary>
    public static void ColliderDrawer(Vector3 centerPos,Quaternion rotation,float scale, ColliderConfig colliderConfig, Color centerColor, Color colliderColor)
    {
        // 1. 计算偏移后的世界中心
        // 逻辑：先旋转本地偏移向量，然后整体乘以 scale
        Vector3 localOffset = (Vector3)colliderConfig.offset;
        Vector3 rotatedOffset = rotation * localOffset;

        // 均匀缩放：直接乘标量
        Vector3 offsetWorld = rotatedOffset * scale;
        Vector3 centerOffseted = centerPos + offsetWorld;
        Gizmos.color = centerColor;

        // 中心点大小也随缩放变化，防止物体太大时点太小看不见，或物体太小时点太大
        float dotSize = 0.01f * scale;
        // 限制最小大小，避免 scale 为 0 或极小时看不见
        if (dotSize < 0.005f) dotSize = 0.005f;

        Gizmos.DrawSphere(centerPos, dotSize);
        Gizmos.DrawLine(centerPos, centerOffseted);
        Gizmos.DrawSphere(centerOffseted, dotSize);

        // --- 绘制碰撞器 ---
        Gizmos.color = colliderColor;

        switch (colliderConfig.shape)
        {
            case E_ColliderShape.None:
                break;

            case E_ColliderShape.Rect:
                Matrix4x4 originalMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(centerOffseted, rotation, Vector3.one * scale);
                Gizmos.DrawWireCube(Vector3.zero, colliderConfig.boxSize);
                Gizmos.matrix = originalMatrix;
                break;

            case E_ColliderShape.Circle:
                float scaledRadius = colliderConfig.radius * scale;
                Gizmos.DrawWireSphere(centerOffseted, scaledRadius);

                // 绘制方向指示线 (长度也受缩放影响)
                Vector3 forwardDir = rotation * Vector3.right;
                Gizmos.DrawLine(centerOffseted, centerOffseted + forwardDir * scaledRadius);
                break;
        }
    }
}
