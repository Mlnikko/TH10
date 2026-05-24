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
            int playerEntityIdx = indices[i];

            ref var player = ref players[playerEntityIdx];
            var input = InputManager.Instance.GetInputForFrame(player.playerIndex, currentframe);
            player.isSlowMode = input.SlowMode;
            player.isShooting = input.Shoot;
            player.isBombing = input.Bomb;

            float distancePerFrame = input.SlowMode ? player.moveSlowDistancePerFrame : player.moveDistancePerFrame;
            float dx = input.MoveHorizontal * distancePerFrame;
            float dy = input.MoveVertical * distancePerFrame;

            ref var vel = ref velocities[playerEntityIdx];
            vel.vx = input.MoveHorizontal * distancePerFrame;
            vel.vy = input.MoveVertical * distancePerFrame;

            ref var pos = ref positions[playerEntityIdx];
            pos.x += dx;
            pos.y += dy;

            pos.x = Mathf.Clamp(pos.x, GlobalBattleData.AreaData.Left, GlobalBattleData.AreaData.Right);
            pos.y = Mathf.Clamp(pos.y, GlobalBattleData.AreaData.Bottom, GlobalBattleData.AreaData.Top);

            TrySyncWeaponPowerSecondaries(playerEntityIdx, ref player);
            TrySyncWeaponEmitterLayout(playerEntityIdx, ref player, emitters);
        }

        SyncOwnedEmitters(positions, rotations, players, emitters);
    }

    void TrySyncWeaponPowerSecondaries(int playerEntityIdx, ref CPlayer player)
    {
        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig == null)
            return;

        Entity playerEntity = EntityManager.GetEntity(playerEntityIdx);
        EntityFactory.SyncPlayerSecondaryEmitters(playerEntity, weaponConfig, player.powerOrbs);
    }

    void TrySyncWeaponEmitterLayout(int playerEntityIdx, ref CPlayer player, Span<CDanmakuEmitter> emitters)
    {
        byte layoutVariant = player.isSlowMode ? (byte)1 : (byte)0;
        if (layoutVariant == player.emitterSlotLayoutVariant)
            return;

        player.emitterSlotLayoutVariant = layoutVariant;

        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig == null)
            return;

        var ownerships = EntityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        Span<int> ownedIndices = EntityManager.GetActiveIndices<CPlayerEmitterOwnership>();

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != playerEntityIdx)
                continue;

            RebuildOwnedEmitter(emitterIdx, ref player, weaponConfig, ownerships, emitters);
        }
    }

    static void RebuildOwnedEmitter(
        int emitterEntityIndex,
        ref CPlayer player,
        WeaponConfig weaponConfig,
        Span<CPlayerEmitterOwnership> ownerships,
        Span<CDanmakuEmitter> emitters)
    {
        if ((uint)emitterEntityIndex >= (uint)emitters.Length)
            return;

        ref var ownership = ref ownerships[emitterEntityIndex];

        int emitterCfgIndex;
        Vector2 slotOffset;

        if (ownership.role == E_WeaponEmitterSlotRole.Primary)
        {
            emitterCfgIndex = weaponConfig.ResolvePrimaryEmitterCfgIndex(player.isSlowMode);
            slotOffset = weaponConfig.ResolvePrimarySlotOffset(player.isSlowMode);
        }
        else
        {
            int secIndex = ownership.secondarySlotIndex;
            if (!weaponConfig.TryResolvePowerSecondary(player.powerOrbs, out var tier))
                return;

            var indices = tier.emitterCfgIndices;
            if (indices == null || secIndex < 0 || secIndex >= indices.Length)
                return;

            emitterCfgIndex = indices[secIndex];
            var baseSlot = new Vector2(ownership.slotOffsetX, ownership.slotOffsetY);
            slotOffset = weaponConfig.ResolveSecondarySlotOffset(baseSlot, player.isSlowMode);
        }

        if (emitterCfgIndex < 0)
            return;

        var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterCfgIndex);
        if (emitterCfg == null)
            return;

        bool wasEmitting = emitters[emitterEntityIndex].isEmitting;
        uint lastFireFrame = emitters[emitterEntityIndex].lastFireFrame;

        var replacement = new CDanmakuEmitter(emitterCfg);
        replacement.emitterPosOffsetX += slotOffset.x;
        replacement.emitterPosOffsetY += slotOffset.y;
        replacement.isEmitting = wasEmitting;
        replacement.lastFireFrame = lastFireFrame;
        emitters[emitterEntityIndex] = replacement;
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
