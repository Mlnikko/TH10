using System;
using UnityEngine;

public class PlayerControlSystem : BaseSystem
{
    public override void OnLogicTick(uint currentframe)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CPlayer>();

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();
        var players = EntityManager.GetComponentSpan<CPlayer>();
        var emitters = EntityManager.GetComponentSpan<CDanmakuEmitter>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];

            ref var player = ref players[idx];
            var input = InputManager.Instance.GetInputForFrame(player.playerIndex, currentframe);
            // 更新运行时状态
            player.isSlowMode = input.SlowMode;
            player.isShooting = input.Shoot;
            player.isBombing = input.Bomb;

            float distancePerFrame = input.SlowMode ? player.moveSlowDistancePerFrame : player.moveDistancePerFrame;
            float dx = input.MoveHorizontal * distancePerFrame;
            float dy = input.MoveVertical * distancePerFrame;

            ref var vel = ref velocities[idx];
            vel.vx = input.MoveHorizontal * distancePerFrame;
            vel.vy = input.MoveVertical * distancePerFrame;

            ref var pos = ref positions[idx];
            pos.x += dx;
            pos.y += dy;

            // === 限制在战斗区域内 ===
            pos.x = Mathf.Clamp(pos.x, GlobalBattleData.AreaData.Left, GlobalBattleData.AreaData.Right);
            pos.y = Mathf.Clamp(pos.y, GlobalBattleData.AreaData.Bottom, GlobalBattleData.AreaData.Top);

            ref var emitter = ref emitters[idx];
            emitter.isEmitting = player.isShooting;
        }
    }
}