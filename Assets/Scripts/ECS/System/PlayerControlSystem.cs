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

            if (EntityManager.HasComponent<CPoolRecycleTag>(playerEntityIdx))
                continue;

            ref var player = ref players[playerEntityIdx];

            if (player.invincibleFramesRemaining > 0)
                player.invincibleFramesRemaining--;
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

            PlayerMoveBounds.ClampAnchorToBattleArea(
                ref pos.x,
                ref pos.y,
                GlobalBattleData.AreaData,
                player.moveColliderShape,
                player.moveColliderOffsetX,
                player.moveColliderOffsetY,
                player.moveColliderRadius,
                player.moveColliderHalfW,
                player.moveColliderHalfH);

            var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
            if (weaponConfig != null)
            {
                EnsureAndRecordPlayerMotionTrail(playerEntityIdx, weaponConfig, pos.x, pos.y);
                EntityFactory.SyncPlayerWeaponPowerLayouts(
                    EntityManager.GetEntity(playerEntityIdx),
                    weaponConfig,
                    player.powerOrbs);
            }
            TrySyncWeaponPrimaryEmitterLayout(playerEntityIdx, ref player, emitters);
            AnimateSecondarySlotConvergence(playerEntityIdx, ref player, emitters);
        }

        SyncOwnedEmitters(positions, rotations, players, emitters);
    }

    void TrySyncWeaponPrimaryEmitterLayout(int playerEntityIdx, ref CPlayer player, Span<CDanmakuEmitter> emitters)
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
            if (ownership.role != E_WeaponEmitterSlotRole.Primary)
                continue;

            RebuildOwnedEmitter(emitterIdx, ref player, weaponConfig, ownerships, emitters);
        }

        player.appliedPrimarySlowPowerMinOrbs = player.isSlowMode
            && weaponConfig.TryResolvePowerPrimarySlow(player.powerOrbs, out var slowTier)
            ? slowTier.minPowerOrbs
            : int.MinValue;
    }

    void AnimateSecondarySlotConvergence(int playerEntityIdx, ref CPlayer player, Span<CDanmakuEmitter> emitters)
    {
        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig == null)
            return;

        float targetT = player.isSlowMode ? 1f : 0f;
        float speed = weaponConfig.slowModeLayout.secondarySlotConvergeSpeed;

        if (speed <= 0f)
            player.secondarySlotConvergeT = targetT;
        else
        {
            uint fps = GameManager.logicFPS > 0 ? (uint)GameManager.logicFPS : 60;
            float step = speed / fps;
            player.secondarySlotConvergeT = Mathf.MoveTowards(player.secondarySlotConvergeT, targetT, step);
        }

        ApplySecondaryEmitterSlotOffsets(playerEntityIdx, player.secondarySlotConvergeT, weaponConfig, emitters);
    }

    void ApplySecondaryEmitterSlotOffsets(
        int playerEntityIdx,
        float converge01,
        WeaponConfig weaponConfig,
        Span<CDanmakuEmitter> emitters)
    {
        Span<int> ownedIndices = EntityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownershipSpan = EntityManager.GetComponentSpan<CPlayerEmitterOwnership>();

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            if ((uint)emitterIdx >= (uint)emitters.Length)
                continue;

            ref var ownership = ref ownershipSpan[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != playerEntityIdx)
                continue;
            if (ownership.role != E_WeaponEmitterSlotRole.Secondary)
                continue;

            Vector2 baseSlot = new(ownership.slotOffsetX, ownership.slotOffsetY);
            ref readonly var player = ref EntityManager.GetComponentSpan<CPlayer>()[playerEntityIdx];
            Vector2 slotOffset = weaponConfig.ResolveSecondaryEmitterSlotOffset(
                baseSlot,
                converge01,
                player.isSlowMode);

            ref var emitter = ref emitters[emitterIdx];
            emitter.emitterPosOffsetX = ownership.emitterBaseOffsetX + slotOffset.x;
            emitter.emitterPosOffsetY = ownership.emitterBaseOffsetY + slotOffset.y;
        }
    }

    void EnsureAndRecordPlayerMotionTrail(
        int playerEntityIdx,
        WeaponConfig weaponConfig,
        float x,
        float y)
    {
        if (weaponConfig == null || !weaponConfig.UsesSecondaryTrailFollow())
            return;

        int capacity = weaponConfig.ResolveSecondaryTrailCapacityFrames();
        if (!EntityManager.HasComponent<CPlayerMotionTrail>(playerEntityIdx))
        {
            EntityManager.AddComponent(
                playerEntityIdx,
                CPlayerMotionTrail.Create(capacity, x, y));
        }

        ref var trail = ref EntityManager.GetComponentSpan<CPlayerMotionTrail>()[playerEntityIdx];
        if (!trail.IsValid || trail.Capacity < capacity)
            trail = CPlayerMotionTrail.Create(capacity, x, y);

        trail.Record(x, y);
    }

    public static void RebuildOwnedEmitter(
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
            emitterCfgIndex = weaponConfig.ResolvePrimaryEmitterCfgIndex(player.isSlowMode, player.powerOrbs);
            slotOffset = weaponConfig.ResolvePrimarySlotOffset(player.isSlowMode, player.powerOrbs);
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
            slotOffset = weaponConfig.ResolveSecondaryEmitterSlotOffset(
                baseSlot,
                player.secondarySlotConvergeT,
                player.isSlowMode);
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

        ownership.emitterBaseOffsetX = emitterCfg.emitterPosOffset.x;
        ownership.emitterBaseOffsetY = emitterCfg.emitterPosOffset.y;
        ownership.emitterCfgIndex = emitterCfgIndex;
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
            if (!TrySampleSecondaryTrailAnchor(
                    ownerIdx,
                    in player,
                    in ownership,
                    out float trailX,
                    out float trailY))
            {
                trailX = ownerPos.x;
                trailY = ownerPos.y;
            }

            emitterPos.x = trailX;
            emitterPos.y = trailY;

            rotations[emitterIdx] = ownerRot;

            ref var emitter = ref emitters[emitterIdx];
            emitter.isEmitting = player.isShooting;
        }
    }

    bool TrySampleSecondaryTrailAnchor(
        int ownerPlayerEntityIndex,
        in CPlayer player,
        in CPlayerEmitterOwnership ownership,
        out float x,
        out float y)
    {
        x = y = 0f;
        if (ownership.role != E_WeaponEmitterSlotRole.Secondary)
            return false;

        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig == null || !weaponConfig.ShouldUseSecondaryTrail(player.isSlowMode))
            return false;
        if (!EntityManager.HasComponent<CPlayerMotionTrail>(ownerPlayerEntityIndex))
            return false;

        ref var trail = ref EntityManager.GetComponentSpan<CPlayerMotionTrail>()[ownerPlayerEntityIndex];
        int framesAgo = weaponConfig.ResolveSecondaryTrailDelayFrames(ownership.secondarySlotIndex);
        return trail.TrySampleFramesAgo(framesAgo, out x, out y);
    }
}
