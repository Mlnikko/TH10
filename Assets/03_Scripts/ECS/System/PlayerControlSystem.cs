using System;
using UnityEngine;

/// <summary>
/// 玩家控制系统：处理输入 → 更新位置 → 触发射击
/// 帧同步安全：所有逻辑在 FixedUpdate 中执行，输入来自 InputManager 的逻辑帧缓冲
/// </summary>
public class PlayerControlSystem : BaseSystem
{
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        Span<int> playerIndices = stackalloc int[4];
        int count = EntityManager.GetEntities<CPlayer>(playerIndices);

        var players = EntityManager.GetComponentSpan<CPlayer>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var runtimes = EntityManager.GetComponentSpan<CPlayerRunTime>();
        var playerAttr = EntityManager.GetComponentSpan<CPlayerAttribute>();

        for (byte i = 0; i < count; i++)
        {
            int entityIndex = playerIndices[i];
            byte playerIndex = players[entityIndex].playerIndex;

            if (!InputManager.Instance.TryGetInputForFrame(playerIndex, BattleTimer.CurrentLogicFrame, out var input))
            {
                input = FrameInput.Default;
            }

            ref var pos = ref positions[entityIndex];
            ref var vel = ref velocities[entityIndex];
            ref var runtime = ref runtimes[entityIndex];
            ref var attr = ref playerAttr[entityIndex];

            // 更新运行时状态
            runtime.isSlowMode = input.SlowMode;
            runtime.isShooting = input.Shoot;

            // 计算速度
            float speed = input.SlowMode ? attr.moveSlowSpeed : attr.moveSpeed;
            vel.vx = input.MoveHorizontal * speed;
            vel.vy = input.MoveVertical * speed;

            // === 积分位置（显式 Euler）===
            pos.x += vel.vx * fixedDeltaTime;
            pos.y += vel.vy * fixedDeltaTime;

            // === 限制在战斗区域内 ===
            pos.x = Mathf.Clamp(pos.x, GlobalBattleArea.Data.Left, GlobalBattleArea.Data.Right);
            pos.y = Mathf.Clamp(pos.y, GlobalBattleArea.Data.Bottom, GlobalBattleArea.Data.Top);

            // TODO: 触发射击系统
        }
    }
}