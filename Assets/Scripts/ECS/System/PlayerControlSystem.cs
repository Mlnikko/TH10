using System;
using UnityEngine;

/// <summary>
/// 玩家控制系统：处理输入 → 更新位置 → 触发射击
/// 帧同步安全：所有逻辑在 LogicTick 中执行，输入来自 InputManager 的逻辑帧缓冲
/// </summary>
public class PlayerControlSystem : BaseSystem
{
    public override void OnLogicTick(uint currentTick)
    {
        Span<int> playerIndices = stackalloc int[4];
        int playerCount = EntityManager.GetEntities<CPlayer>(playerIndices);
        var players = EntityManager.GetComponentSpan<CPlayer>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();
        var positions = EntityManager.GetComponentSpan<CPosition>();
        var playerRuntimes = EntityManager.GetComponentSpan<CPlayerRunTime>();
        var playerAttr = EntityManager.GetComponentSpan<CPlayerAttribute>();
        var emitters = EntityManager.GetComponentSpan<CDanmakuEmitter>();

        for (int i = 0; i < playerCount; i++)
        {
            int entityIndex = playerIndices[i];
            byte playerIndex = players[entityIndex].playerIndex;

            var input = InputManager.Instance.GetInputForFrame(playerIndex, currentTick);

            ref var pos = ref positions[entityIndex];
            ref var vel = ref velocities[entityIndex];
            ref var runtime = ref playerRuntimes[entityIndex];
            ref var attr = ref playerAttr[entityIndex];
            ref var emitter = ref emitters[entityIndex];

            // 更新运行时状态
            runtime.isSlowMode = input.SlowMode;
            runtime.isShooting = input.Shoot;
            runtime.isBombing = input.Bomb;

            // 根据速度模式选择移动速度
            float speedPerFrame = input.SlowMode ? attr.moveSlowSpeedPerFrame : attr.moveSpeedPerFrame;

            float dx = input.MoveHorizontal * speedPerFrame;
            float dy = input.MoveVertical * speedPerFrame;

            vel.vx = dx;
            vel.vy = dy;

            pos.x += dx;
            pos.y += dy;

            // === 限制在战斗区域内 ===
            pos.x = Mathf.Clamp(pos.x, GlobalBattleData.AreaData.Left, GlobalBattleData.AreaData.Right);
            pos.y = Mathf.Clamp(pos.y, GlobalBattleData.AreaData.Bottom, GlobalBattleData.AreaData.Top);

            emitter.isEnabled = input.Shoot;

            if (input.Bomb)
            {

            }
        }
    }
}