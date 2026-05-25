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

            var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
            if (weaponConfig != null)
            {
                EntityFactory.SyncPlayerWeaponPowerLayouts(
                    EntityManager.GetEntity(playerEntityIdx),
                    weaponConfig,
                    player.powerOrbs);
            }
            TrySyncWeaponPrimaryEmitterLayout(
                playerEntityIdx,
                ref player,
                positions,
                velocities,
                emitters);
            UpdateSecondarySlowPositioning(
                playerEntityIdx,
                ref player,
                velocities,
                emitters);
        }

        SyncOwnedEmitters(positions, rotations, players, emitters);
    }

    void TrySyncWeaponPrimaryEmitterLayout(
        int playerEntityIdx,
        ref CPlayer player,
        Span<CPosition> positions,
        Span<CVelocity> velocities,
        Span<CDanmakuEmitter> emitters)
    {
        byte layoutVariant = player.isSlowMode ? (byte)1 : (byte)0;
        bool slowModeChanged = layoutVariant != player.emitterSlotLayoutVariant;

        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig == null)
            return;

        if (slowModeChanged)
        {
            player.emitterSlotLayoutVariant = layoutVariant;
            HandleSecondarySlowModeTransition(
                playerEntityIdx,
                ref player,
                weaponConfig,
                positions,
                emitters);
        }

        Span<int> ownedIndices = EntityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = EntityManager.GetComponentSpan<CPlayerEmitterOwnership>();

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != playerEntityIdx)
                continue;
            if (ownership.role != E_WeaponEmitterSlotRole.Primary)
                continue;

            if (slowModeChanged)
                RebuildOwnedEmitter(emitterIdx, ref player, weaponConfig, ownerships, emitters);
        }

        player.appliedPrimarySlowPowerMinOrbs = player.isSlowMode
            && weaponConfig.TryResolvePowerPrimarySlow(player.powerOrbs, out var slowTier)
            ? slowTier.minPowerOrbs
            : int.MinValue;
    }

    void HandleSecondarySlowModeTransition(
        int playerEntityIdx,
        ref CPlayer player,
        WeaponConfig weaponConfig,
        Span<CPosition> positions,
        Span<CDanmakuEmitter> emitters)
    {
        var layout = weaponConfig.slowModeLayout ?? new WeaponSlowModeLayoutConfig();
        var mode = layout.secondarySlowPositionMode;

        Span<int> ownedIndices = EntityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = EntityManager.GetComponentSpan<CPlayerEmitterOwnership>();

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != playerEntityIdx)
                continue;
            if (ownership.role != E_WeaponEmitterSlotRole.Secondary)
                continue;

            Vector2 configSlot = new(ownership.slotOffsetX, ownership.slotOffsetY);

            if (player.isSlowMode)
            {
                if (mode == E_WeaponSlowSlotPositionMode.WorldAnchorWhileSlow)
                {
                    ref var emitter = ref emitters[emitterIdx];
                    ref readonly var ownerPos = ref positions[playerEntityIdx];
                    float rotRad = EntityManager.GetComponentSpan<CRotation>()[playerEntityIdx].angleRad;
                    Vector2 total = new Vector2(emitter.emitterPosOffsetX, emitter.emitterPosOffsetY);
                    Vector2 rotated = WeaponEmitLayout.RotateOffset(total, rotRad);

                    ownership.slowWorldAnchorX = ownerPos.x + rotated.x;
                    ownership.slowWorldAnchorY = ownerPos.y + rotated.y;
                    ownership.slowPositionState |= WeaponSlowModePosition.SlowStateWorldAnchor;

                    ref var pos = ref positions[emitterIdx];
                    pos.x = ownership.slowWorldAnchorX;
                    pos.y = ownership.slowWorldAnchorY;

                    emitter.emitterPosOffsetX = ownership.emitterBaseOffsetX;
                    emitter.emitterPosOffsetY = ownership.emitterBaseOffsetY;
                }

                player.secondarySlotConvergeT = mode == E_WeaponSlowSlotPositionMode.ConvergeToPlayer ? 1f : 0f;
            }
            else
            {
                if ((ownership.slowPositionState & WeaponSlowModePosition.SlowStateWorldAnchor) != 0)
                {
                    ownership.slowPositionState &= unchecked((byte)~WeaponSlowModePosition.SlowStateWorldAnchor);
                    ref var pos = ref positions[emitterIdx];
                    ref var ownerPos = ref positions[playerEntityIdx];
                    pos.x = ownerPos.x;
                    pos.y = ownerPos.y;

                    Vector2 worldDelta = new(
                        ownership.slowWorldAnchorX - ownerPos.x,
                        ownership.slowWorldAnchorY - ownerPos.y);
                    float rotRad = EntityManager.GetComponentSpan<CRotation>()[playerEntityIdx].angleRad;
                    float cos = Mathf.Cos(-rotRad);
                    float sin = Mathf.Sin(-rotRad);
                    ownership.runtimeSlotOffsetX = worldDelta.x * cos - worldDelta.y * sin;
                    ownership.runtimeSlotOffsetY = worldDelta.x * sin + worldDelta.y * cos;
                }

                player.secondarySlotConvergeT = 0f;
            }

            ApplySecondaryEmitterOffsetForOwnership(
                emitterIdx,
                ref ownership,
                ref player,
                weaponConfig,
                emitters);
        }
    }

    void UpdateSecondarySlowPositioning(
        int playerEntityIdx,
        ref CPlayer player,
        Span<CVelocity> velocities,
        Span<CDanmakuEmitter> emitters)
    {
        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig == null)
            return;

        var layout = weaponConfig.slowModeLayout ?? new WeaponSlowModeLayoutConfig();
        var mode = layout.secondarySlowPositionMode;
        uint fps = GameManager.logicFPS > 0 ? (uint)GameManager.logicFPS : 60;

        if (mode == E_WeaponSlowSlotPositionMode.ConvergeToPlayer)
        {
            float targetT = player.isSlowMode ? 1f : 0f;
            float speed = layout.secondarySlotConvergeSpeed;
            if (speed <= 0f)
                player.secondarySlotConvergeT = targetT;
            else
            {
                float step = speed / fps;
                player.secondarySlotConvergeT = Mathf.MoveTowards(player.secondarySlotConvergeT, targetT, step);
            }
        }
        else if (!player.isSlowMode)
        {
            ref readonly var vel = ref velocities[playerEntityIdx];
            Span<int> ownedIndices = EntityManager.GetActiveIndices<CPlayerEmitterOwnership>();
            var ownerships = EntityManager.GetComponentSpan<CPlayerEmitterOwnership>();

            for (int i = 0; i < ownedIndices.Length; i++)
            {
                int emitterIdx = ownedIndices[i];
                ref var ownership = ref ownerships[emitterIdx];
                if (ownership.ownerPlayerEntityIndex != playerEntityIdx)
                    continue;
                if (ownership.role != E_WeaponEmitterSlotRole.Secondary)
                    continue;

                Vector2 configSlot = new(ownership.slotOffsetX, ownership.slotOffsetY);
                Vector2 runtime = new(ownership.runtimeSlotOffsetX, ownership.runtimeSlotOffsetY);

                if (mode == E_WeaponSlowSlotPositionMode.TrailFollowWhileFast)
                {
                    WeaponSlowModePosition.StepTrailFollowOffset(
                        ref runtime,
                        configSlot,
                        vel.vx,
                        vel.vy,
                        layout,
                        fps);
                }
                else
                {
                    WeaponSlowModePosition.StepReturnToConfigSlot(
                        ref runtime,
                        configSlot,
                        layout,
                        fps);
                }

                ownership.runtimeSlotOffsetX = runtime.x;
                ownership.runtimeSlotOffsetY = runtime.y;
            }
        }

        ApplySecondaryEmitterSlotOffsets(playerEntityIdx, ref player, weaponConfig, emitters);
    }

    void ApplySecondaryEmitterSlotOffsets(
        int playerEntityIdx,
        ref CPlayer player,
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

            ApplySecondaryEmitterOffsetForOwnership(
                emitterIdx,
                ref ownership,
                ref player,
                weaponConfig,
                emitters);
        }
    }

    static void ApplySecondaryEmitterOffsetForOwnership(
        int emitterIdx,
        ref CPlayerEmitterOwnership ownership,
        ref CPlayer player,
        WeaponConfig weaponConfig,
        Span<CDanmakuEmitter> emitters)
    {
        if ((ownership.slowPositionState & WeaponSlowModePosition.SlowStateWorldAnchor) != 0
            && player.isSlowMode)
        {
            return;
        }

        Vector2 baseSlot = new(ownership.slotOffsetX, ownership.slotOffsetY);
        Vector2 runtime = new(ownership.runtimeSlotOffsetX, ownership.runtimeSlotOffsetY);
        Vector2 slotOffset = weaponConfig.ResolveSecondarySlotOffset(
            baseSlot,
            player.isSlowMode,
            player.secondarySlotConvergeT,
            runtime);

        ref var emitter = ref emitters[emitterIdx];
        emitter.emitterPosOffsetX = ownership.emitterBaseOffsetX + slotOffset.x;
        emitter.emitterPosOffsetY = ownership.emitterBaseOffsetY + slotOffset.y;
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
            var runtime = new Vector2(ownership.runtimeSlotOffsetX, ownership.runtimeSlotOffsetY);
            slotOffset = weaponConfig.ResolveSecondarySlotOffset(
                baseSlot,
                player.isSlowMode,
                player.secondarySlotConvergeT,
                runtime);
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

            bool worldAnchorSlow = ownership.role == E_WeaponEmitterSlotRole.Secondary
                && player.isSlowMode
                && (ownership.slowPositionState & WeaponSlowModePosition.SlowStateWorldAnchor) != 0;

            if (!worldAnchorSlow)
            {
                emitterPos.x = ownerPos.x;
                emitterPos.y = ownerPos.y;
            }

            rotations[emitterIdx] = ownerRot;

            ref var emitter = ref emitters[emitterIdx];
            emitter.isEmitting = player.isShooting;
        }
    }
}
