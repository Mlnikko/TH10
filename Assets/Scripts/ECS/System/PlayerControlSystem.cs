using System;
using UnityEngine;

public class PlayerControlSystem : BaseSystem
{
    public override void OnLogicTick(uint currentframe)
    {
        Span<int> indices = EntityManager.GetActiveIndices<CPlayer>();

        var positions = EntityManager.GetComponentSpan<CPosition>();
        var velocities = EntityManager.GetComponentSpan<CVelocity>();
        var rotations = EntityManager.GetComponentSpan<CRotation>();
        var players = EntityManager.GetComponentSpan<CPlayer>();
        var emitters = EntityManager.GetComponentSpan<CDanmakuEmitter>();

        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];

            ref var player = ref players[idx];
            var input = InputManager.Instance.GetInputForFrame(player.playerIndex, currentframe);
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

            pos.x = Mathf.Clamp(pos.x, GlobalBattleData.AreaData.Left, GlobalBattleData.AreaData.Right);
            pos.y = Mathf.Clamp(pos.y, GlobalBattleData.AreaData.Bottom, GlobalBattleData.AreaData.Top);

            TrySwapPrimaryEmitter(ref player, emitters);
        }

        SyncOwnedEmitters(positions, rotations, players, emitters);
    }

    void TrySwapPrimaryEmitter(ref CPlayer player, Span<CDanmakuEmitter> emitters)
    {
        if (player.primaryEmitterEntityIndex < 0)
            return;

        byte variant = player.isSlowMode ? (byte)1 : (byte)0;
        if (variant == player.primaryEmitterConfigVariant)
            return;

        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig == null)
            return;

        int emitterCfgIndex = weaponConfig.ResolvePrimaryEmitterCfgIndex(player.isSlowMode);
        if (emitterCfgIndex < 0)
            return;

        var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterCfgIndex);
        if (emitterCfg == null)
            return;

        int emitterEntityIndex = player.primaryEmitterEntityIndex;
        if ((uint)emitterEntityIndex >= (uint)emitters.Length)
            return;

        bool wasEmitting = emitters[emitterEntityIndex].isEmitting;
        uint lastFireFrame = emitters[emitterEntityIndex].lastFireFrame;

        var ownerships = EntityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        ref var ownership = ref ownerships[emitterEntityIndex];

        var replacement = new CDanmakuEmitter(emitterCfg);
        replacement.emitterPosOffsetX += ownership.slotOffsetX;
        replacement.emitterPosOffsetY += ownership.slotOffsetY;
        replacement.isEmitting = wasEmitting;
        replacement.lastFireFrame = lastFireFrame;
        emitters[emitterEntityIndex] = replacement;

        player.primaryEmitterConfigVariant = variant;
    }

    void SyncOwnedEmitters(
        Span<CPosition> positions,
        Span<CRotation> rotations,
        Span<CPlayer> players,
        Span<CDanmakuEmitter> emitters)
    {
        Span<int> ownedIndices = EntityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = EntityManager.GetComponentSpan<CPlayerEmitterOwnership>();

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];

            int ownerIdx = ownership.ownerPlayerEntityIndex;
            if ((uint)ownerIdx >= (uint)players.Length)
                continue;

            ref var player = ref players[ownerIdx];
            ref var ownerPos = ref positions[ownerIdx];
            ref var ownerRot = ref rotations[ownerIdx];

            ref var emitterPos = ref positions[emitterIdx];
            emitterPos.x = ownerPos.x;
            emitterPos.y = ownerPos.y;

            rotations[emitterIdx] = ownerRot;

            ref var emitter = ref emitters[emitterIdx];
            emitter.isEmitting = player.isShooting;
        }
    }
}
