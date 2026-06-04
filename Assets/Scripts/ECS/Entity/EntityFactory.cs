using System;
using UnityEngine;

public class EntityFactory
{
    readonly EntityManager _entityManager;

    public EntityFactory(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }
   
    public Entity CreatePlayer(PlayerBattleData playerBattleData, float posX, float posY)
    {
        Entity e_player = _entityManager.CreateEntity();

        string characterId = StringHelper.NormalizeResourceId(playerBattleData.characterId.ToString());
        string weaponId = StringHelper.NormalizeResourceId(playerBattleData.weaponId.ToString());

        var characterConfig = GameResDB.Instance.GetConfig<CharacterConfig>(characterId);
        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(weaponId);

        if (characterConfig == null)
        {
            Logger.Error($"CharacterConfig not found for Key: {characterId}");
            return Entity.Null;
        }

        if (weaponConfig == null)
        {
            Logger.Error($"WeaponConfig not found for Key: {weaponId}");
            return Entity.Null;
        }

        int characterCfgIndex = GameResDB.Instance.GetConfigIndex(characterId);
        int weaponCfgIndex = GameResDB.Instance.GetConfigIndex(weaponId);
        if (characterCfgIndex < 0 || characterCfgIndex > byte.MaxValue)
        {
            Logger.Error($"CharacterConfig index invalid for Key: {characterId} (index={characterCfgIndex})");
            return Entity.Null;
        }
        if (weaponCfgIndex < 0 || weaponCfgIndex > byte.MaxValue)
        {
            Logger.Error($"WeaponConfig index invalid for Key: {weaponId} (index={weaponCfgIndex})");
            return Entity.Null;
        }

        PlayerMoveBounds.BakeFromConfig(
            characterConfig.moveColliderConfig,
            out byte moveShape,
            out float moveOffsetX,
            out float moveOffsetY,
            out float moveRadius,
            out float moveHalfW,
            out float moveHalfH);

        _entityManager.AddComponent(e_player, new CPosition(posX, posY));
        _entityManager.AddComponent(e_player, new CRotation(0));
        _entityManager.AddComponent(e_player, new CVelocity(0, 0));
        _entityManager.AddComponent(e_player, new CPlayer()
        {
            playerIndex = playerBattleData.playerIndex,
            characterCfgIndex = (byte)characterCfgIndex,
            weaponCfgIndex = (byte)weaponCfgIndex,

            moveDistancePerFrame = characterConfig.moveDistancePerFrame,
            moveSlowDistancePerFrame = characterConfig.moveSlowDistancePerFrame,

            hitRadius = characterConfig.hitColliderConfig.radius,
            grazeRadius = characterConfig.grazeColliderConfig.radius,

            moveColliderShape = moveShape,
            moveColliderOffsetX = moveOffsetX,
            moveColliderOffsetY = moveOffsetY,
            moveColliderRadius = moveRadius,
            moveColliderHalfW = moveHalfW,
            moveColliderHalfH = moveHalfH,

            isShooting = false,
            isSlowMode = false,
            isBombing = false,
            isInvincible = false,
            invincibleFramesRemaining = 0,
            powerOrbs = 0,
            primaryEmitterEntityIndex = -1,
            emitterSlotLayoutVariant = 0,
            secondarySlotConvergeT = 0f,
            appliedSecondaryPowerMinOrbs = int.MinValue,
            appliedPrimarySlowPowerMinOrbs = int.MinValue,
        });
        _entityManager.AddComponent(e_player, new CHealth(characterConfig.maxHealth, characterConfig.maxHealth));
        _entityManager.AddComponent(e_player, new CCollider
        {
            isActive = true,
            shape = characterConfig.hitColliderConfig.shape,
            layer = characterConfig.hitColliderConfig.layer,
            mask = characterConfig.hitColliderConfig.mask,
            offsetX = characterConfig.hitColliderConfig.offset.x,
            offsetY = characterConfig.hitColliderConfig.offset.y,
            radius = characterConfig.hitColliderConfig.radius,
            width = characterConfig.hitColliderConfig.boxSize.x,
            height = characterConfig.hitColliderConfig.boxSize.y,
        });
        EnsurePlayerMotionTrail(e_player, weaponConfig, posX, posY);

        int playerEntityIndex = e_player.Index;
        ref var playerComponent = ref _entityManager.GetComponent<CPlayer>(e_player);
        int primaryEmitterEntityIndex = CreatePlayerWeaponEmitters(
            playerEntityIndex,
            weaponConfig,
            playerComponent.powerOrbs,
            posX,
            posY);

        playerComponent.primaryEmitterEntityIndex = primaryEmitterEntityIndex;
        playerComponent.appliedSecondaryPowerMinOrbs =
            ResolveSecondaryPowerTierKey(weaponConfig, playerComponent.powerOrbs);

        Logger.Info($"Player {playerBattleData.playerIndex} ({playerBattleData.characterId}) initialized successfully.", LogTag.Battle);
        return e_player;
    }

    int CreatePlayerWeaponEmitters(
        int ownerPlayerEntityIndex,
        WeaponConfig weaponConfig,
        int powerOrbs,
        float posX,
        float posY)
    {
        int primaryEmitterEntityIndex = -1;

        int primaryCfgIndex = weaponConfig.ResolvePrimaryEmitterCfgIndex(slowMode: false);
        if (primaryCfgIndex >= 0)
        {
            primaryEmitterEntityIndex = CreateWeaponEmitterEntity(
                ownerPlayerEntityIndex,
                primaryCfgIndex,
                weaponConfig.primaryEmitters.normal.slotOffset,
                E_WeaponEmitterSlotRole.Primary,
                secondarySlotIndex: 0,
                slowMode: false,
                posX,
                posY);
        }

        SpawnPlayerSecondaryEmitters(
            ownerPlayerEntityIndex,
            weaponConfig,
            powerOrbs,
            slowMode: false,
            posX,
            posY);
        return primaryEmitterEntityIndex;
    }

    /// <summary>按当前 Power 同步副炮（整档替换）与低速主炮配置。</summary>
    public void SyncPlayerWeaponPowerLayouts(Entity playerEntity, WeaponConfig weaponConfig, int powerOrbs)
    {
        if (!_entityManager.IsValid(playerEntity) || weaponConfig == null)
            return;

        ref var player = ref _entityManager.GetComponent<CPlayer>(playerEntity);
        SyncPlayerSecondaryEmittersInternal(playerEntity, weaponConfig, powerOrbs, ref player);
        SyncPlayerPrimarySlowPowerInternal(playerEntity, weaponConfig, powerOrbs, ref player);
    }

    /// <summary>按当前 Power 档位切换副发射器实体（销毁旧档后生成新档，非叠加）。</summary>
    public void SyncPlayerSecondaryEmitters(Entity playerEntity, WeaponConfig weaponConfig, int powerOrbs) =>
        SyncPlayerWeaponPowerLayouts(playerEntity, weaponConfig, powerOrbs);

    void SyncPlayerSecondaryEmittersInternal(
        Entity playerEntity,
        WeaponConfig weaponConfig,
        int powerOrbs,
        ref CPlayer player)
    {
        int tierKey = ResolveSecondaryPowerTierKey(weaponConfig, powerOrbs);
        if (tierKey == player.appliedSecondaryPowerMinOrbs)
            return;

        player.appliedSecondaryPowerMinOrbs = tierKey;

        if (weaponConfig.UsesSecondaryTrailFollow())
        {
            SyncPlayerTrailSecondaryEmittersInternal(playerEntity, weaponConfig, powerOrbs, ref player);
            return;
        }

        DestroyPlayerSecondaryEmitters(playerEntity.Index);

        ref var pos = ref _entityManager.GetComponent<CPosition>(playerEntity);
        SpawnPlayerSecondaryEmitters(playerEntity.Index, weaponConfig, powerOrbs, player.isSlowMode, pos.x, pos.y);
    }

    void SyncPlayerPrimarySlowPowerInternal(
        Entity playerEntity,
        WeaponConfig weaponConfig,
        int powerOrbs,
        ref CPlayer player)
    {
        int tierKey = player.isSlowMode
            ? ResolvePrimarySlowPowerTierKey(weaponConfig, powerOrbs)
            : int.MinValue;

        if (tierKey == player.appliedPrimarySlowPowerMinOrbs)
            return;

        player.appliedPrimarySlowPowerMinOrbs = tierKey;

        if (!player.isSlowMode)
            return;

        RebuildPlayerPrimaryEmitters(playerEntity.Index, weaponConfig, ref player);
    }

    static int ResolvePrimarySlowPowerTierKey(WeaponConfig weaponConfig, int powerOrbs) =>
        weaponConfig.TryResolvePowerPrimarySlow(powerOrbs, out var tier)
            ? tier.minPowerOrbs
            : int.MinValue;

    void RebuildPlayerPrimaryEmitters(int playerEntityIndex, WeaponConfig weaponConfig, ref CPlayer player)
    {
        Span<int> ownedIndices = _entityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        var ownerships = _entityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        var emitters = _entityManager.GetComponentSpan<CDanmakuEmitter>();

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != playerEntityIndex)
                continue;
            if (ownership.role != E_WeaponEmitterSlotRole.Primary)
                continue;

            PlayerControlSystem.RebuildOwnedEmitter(emitterIdx, ref player, weaponConfig, ownerships, emitters);
        }
    }

    static int ResolveSecondaryPowerTierKey(WeaponConfig weaponConfig, int powerOrbs)
    {
        return weaponConfig.TryResolvePowerSecondary(powerOrbs, out var tier)
            ? tier.minPowerOrbs
            : int.MinValue;
    }

    void EnsurePlayerMotionTrail(Entity playerEntity, WeaponConfig weaponConfig, float posX, float posY)
    {
        if (!_entityManager.IsValid(playerEntity) || weaponConfig == null || !weaponConfig.UsesSecondaryTrailFollow())
            return;

        int capacity = weaponConfig.ResolveSecondaryTrailCapacityFrames();
        if (!_entityManager.HasComponent<CPlayerMotionTrail>(playerEntity))
        {
            _entityManager.AddComponent(playerEntity, CPlayerMotionTrail.Create(capacity, posX, posY));
            return;
        }

        ref var trail = ref _entityManager.GetComponent<CPlayerMotionTrail>(playerEntity);
        if (!trail.IsValid || trail.Capacity < capacity)
            trail = CPlayerMotionTrail.Create(capacity, posX, posY);
    }

    bool TryResolveSecondaryTrailAnchor(
        int playerEntityIndex,
        WeaponConfig weaponConfig,
        bool slowMode,
        byte queueIndex,
        ref float x,
        ref float y)
    {
        if (weaponConfig == null || !weaponConfig.ShouldUseSecondaryTrail(slowMode))
            return false;
        if (!_entityManager.HasComponent<CPlayerMotionTrail>(playerEntityIndex))
            return false;

        ref var trail = ref _entityManager.GetComponentSpan<CPlayerMotionTrail>()[playerEntityIndex];
        int framesAgo = weaponConfig.ResolveSecondaryTrailDelayFrames(queueIndex);
        if (!trail.TrySampleFramesAgo(framesAgo, out float sampleX, out float sampleY))
            return false;

        x = sampleX;
        y = sampleY;
        return true;
    }

    void SyncPlayerTrailSecondaryEmittersInternal(
        Entity playerEntity,
        WeaponConfig weaponConfig,
        int powerOrbs,
        ref CPlayer player)
    {
        ref var pos = ref _entityManager.GetComponent<CPosition>(playerEntity);
        EnsurePlayerMotionTrail(playerEntity, weaponConfig, pos.x, pos.y);

        if (!weaponConfig.TryResolvePowerSecondary(powerOrbs, out var tier))
        {
            DestroyPlayerSecondaryEmitters(playerEntity.Index);
            return;
        }

        var indices = tier.emitterCfgIndices;
        var slots = tier.slots;
        int desiredCount = indices != null && slots != null
            ? Mathf.Min(indices.Length, slots.Length)
            : 0;

        if (desiredCount <= 0)
        {
            DestroyPlayerSecondaryEmitters(playerEntity.Index);
            return;
        }

        var ownedSecondaryIndices = new System.Collections.Generic.List<int>(desiredCount);
        CollectPlayerSecondaryEmitterIndices(playerEntity.Index, ownedSecondaryIndices);
        ownedSecondaryIndices.Sort(CompareSecondaryEmitterQueueOrder);

        var ownerships = _entityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        var emitters = _entityManager.GetComponentSpan<CDanmakuEmitter>();
        int keepCount = Mathf.Min(ownedSecondaryIndices.Count, desiredCount);

        for (int i = 0; i < keepCount; i++)
        {
            int emitterIdx = ownedSecondaryIndices[i];
            if (indices[i] < 0)
                continue;

            ref var ownership = ref ownerships[emitterIdx];
            bool configChanged = ownership.emitterCfgIndex != indices[i];
            ownership.secondarySlotIndex = (byte)i;
            ownership.slotOffsetX = slots[i].slotOffset.x;
            ownership.slotOffsetY = slots[i].slotOffset.y;

            if (configChanged)
                PlayerControlSystem.RebuildOwnedEmitter(emitterIdx, ref player, weaponConfig, ownerships, emitters);
            else
                ApplyOwnedSecondaryEmitterSlotOffset(emitterIdx, ref player, weaponConfig, ownerships, emitters);
        }

        for (int i = ownedSecondaryIndices.Count - 1; i >= desiredCount; i--)
        {
            Entity emitterEntity = _entityManager.GetEntity(ownedSecondaryIndices[i]);
            if (!emitterEntity.IsNull)
                _entityManager.DestroyEntity(emitterEntity);
        }

        for (int i = ownedSecondaryIndices.Count; i < desiredCount; i++)
        {
            if (indices[i] < 0)
                continue;

            float spawnX = pos.x;
            float spawnY = pos.y;
            TryResolveSecondaryTrailAnchor(
                playerEntity.Index,
                weaponConfig,
                player.isSlowMode,
                (byte)i,
                ref spawnX,
                ref spawnY);

            CreateWeaponEmitterEntity(
                playerEntity.Index,
                indices[i],
                slots[i].slotOffset,
                E_WeaponEmitterSlotRole.Secondary,
                (byte)i,
                player.isSlowMode,
                spawnX,
                spawnY);
        }
    }

    void CollectPlayerSecondaryEmitterIndices(
        int ownerPlayerEntityIndex,
        System.Collections.Generic.List<int> outIndices)
    {
        outIndices.Clear();

        Span<int> ownedIndices = _entityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = _entityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != ownerPlayerEntityIndex)
                continue;
            if (ownership.role != E_WeaponEmitterSlotRole.Secondary)
                continue;

            outIndices.Add(emitterIdx);
        }
    }

    int CompareSecondaryEmitterQueueOrder(int lhs, int rhs)
    {
        var ownerships = _entityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        int compare = ownerships[lhs].secondarySlotIndex.CompareTo(ownerships[rhs].secondarySlotIndex);
        return compare != 0 ? compare : lhs.CompareTo(rhs);
    }

    static void ApplyOwnedSecondaryEmitterSlotOffset(
        int emitterIdx,
        ref CPlayer player,
        WeaponConfig weaponConfig,
        Span<CPlayerEmitterOwnership> ownerships,
        Span<CDanmakuEmitter> emitters)
    {
        if ((uint)emitterIdx >= (uint)emitters.Length)
            return;

        ref var ownership = ref ownerships[emitterIdx];
        Vector2 baseSlot = new(ownership.slotOffsetX, ownership.slotOffsetY);
        Vector2 slotOffset = weaponConfig.ResolveSecondaryEmitterSlotOffset(
            baseSlot,
            player.secondarySlotConvergeT,
            player.isSlowMode);

        ref var emitter = ref emitters[emitterIdx];
        emitter.emitterPosOffsetX = ownership.emitterBaseOffsetX + slotOffset.x;
        emitter.emitterPosOffsetY = ownership.emitterBaseOffsetY + slotOffset.y;
    }

    void SpawnPlayerSecondaryEmitters(
        int ownerPlayerEntityIndex,
        WeaponConfig weaponConfig,
        int powerOrbs,
        bool slowMode,
        float posX,
        float posY)
    {
        if (!weaponConfig.TryResolvePowerSecondary(powerOrbs, out var tier))
            return;

        var indices = tier.emitterCfgIndices;
        var slots = tier.slots;
        if (indices == null || slots == null)
            return;

        int count = Mathf.Min(indices.Length, slots.Length);
        for (int i = 0; i < count; i++)
        {
            if (indices[i] < 0)
                continue;

            CreateWeaponEmitterEntity(
                ownerPlayerEntityIndex,
                indices[i],
                slots[i].slotOffset,
                E_WeaponEmitterSlotRole.Secondary,
                (byte)i,
                slowMode,
                posX,
                posY);
        }
    }

    public void DestroyPlayerWeaponEmitters(int ownerPlayerEntityIndex)
    {
        Span<int> ownedIndices = _entityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = _entityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        var toDestroy = new System.Collections.Generic.List<Entity>(8);

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != ownerPlayerEntityIndex)
                continue;

            toDestroy.Add(_entityManager.GetEntity(emitterIdx));
        }

        for (int i = 0; i < toDestroy.Count; i++)
            _entityManager.DestroyEntity(toDestroy[i]);
    }

    void DestroyPlayerSecondaryEmitters(int ownerPlayerEntityIndex)
    {
        Span<int> ownedIndices = _entityManager.GetActiveIndices<CPlayerEmitterOwnership>();
        if (ownedIndices.Length == 0)
            return;

        var ownerships = _entityManager.GetComponentSpan<CPlayerEmitterOwnership>();
        var toDestroy = new System.Collections.Generic.List<Entity>(4);

        for (int i = 0; i < ownedIndices.Length; i++)
        {
            int emitterIdx = ownedIndices[i];
            ref var ownership = ref ownerships[emitterIdx];
            if (ownership.ownerPlayerEntityIndex != ownerPlayerEntityIndex)
                continue;
            if (ownership.role != E_WeaponEmitterSlotRole.Secondary)
                continue;

            toDestroy.Add(_entityManager.GetEntity(emitterIdx));
        }

        for (int i = 0; i < toDestroy.Count; i++)
            _entityManager.DestroyEntity(toDestroy[i]);
    }

    int CreateWeaponEmitterEntity(
        int ownerPlayerEntityIndex,
        int emitterCfgIndex,
        Vector2 slotOffset,
        E_WeaponEmitterSlotRole role,
        byte secondarySlotIndex,
        bool slowMode,
        float posX,
        float posY)
    {
        var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(emitterCfgIndex);
        if (emitterCfg == null)
        {
            Logger.Error($"DanmakuEmitter configuration not found for index {emitterCfgIndex}.");
            return -1;
        }

        Entity e_emitter = _entityManager.CreateEntity();
        _entityManager.AddComponent(e_emitter, new CPosition(posX, posY));
        _entityManager.AddComponent(e_emitter, new CRotation(0));

        var emitter = new CDanmakuEmitter(emitterCfg);
        Vector2 appliedSlotOffset = role == E_WeaponEmitterSlotRole.Secondary
            ? ResolveWeaponEmitterSlotOffsetForMode(ownerPlayerEntityIndex, slotOffset, slowMode)
            : slotOffset;
        emitter.emitterPosOffsetX += appliedSlotOffset.x;
        emitter.emitterPosOffsetY += appliedSlotOffset.y;
        _entityManager.AddComponent(e_emitter, emitter);
        _entityManager.AddComponent(e_emitter, new CPlayerEmitterOwnership
        {
            ownerPlayerEntityIndex = ownerPlayerEntityIndex,
            role = role,
            secondarySlotIndex = secondarySlotIndex,
            emitterCfgIndex = emitterCfgIndex,
            slotOffsetX = slotOffset.x,
            slotOffsetY = slotOffset.y,
            emitterBaseOffsetX = emitterCfg.emitterPosOffset.x,
            emitterBaseOffsetY = emitterCfg.emitterPosOffset.y,
        });

        if (PresentationRuntime.SmoothingEnabled)
        {
            _entityManager.AddComponent(
                e_emitter,
                CPresentationPose.FromPosition(posX, posY, 0f, withRotation: true));
        }

        return e_emitter.Index;
    }

    Vector2 ResolveWeaponEmitterSlotOffsetForMode(
        int ownerPlayerEntityIndex,
        Vector2 baseSlotOffset,
        bool slowMode)
    {
        var ownerEntity = _entityManager.GetEntity(ownerPlayerEntityIndex);
        if (ownerEntity.IsNull)
            return baseSlotOffset;

        ref readonly var ownerPlayer = ref _entityManager.GetComponent<CPlayer>(ownerEntity);
        var ownerWeapon = GameResDB.Instance.GetConfig<WeaponConfig>(ownerPlayer.weaponCfgIndex);
        return ownerWeapon != null
            ? ownerWeapon.ResolveSecondaryEmitterSlotOffset(baseSlotOffset, ownerPlayer.secondarySlotConvergeT, slowMode)
            : baseSlotOffset;
    }

    /// <param name="rotationRad">弹幕逻辑旋转（弧度），与 <see cref="CRotation.angleRad"/> 一致。</param>
    public Entity CreateDanmaku(float posX, float posY, float rotationRad, float velX, float velY, int danmakuCfgIndex)
    {
        // 检查配置是否存在
        var danmakuCfg = GameResDB.Instance.GetConfig<DanmakuConfig>(danmakuCfgIndex);

        if (danmakuCfg == null)
        {
            Logger.Error($"Danmaku configuration not found for index {danmakuCfgIndex}.");
            return Entity.Null;
        }

        Entity e_danmaku = _entityManager.CreateEntity();

        _entityManager.AddComponent(e_danmaku, new CDanmaku(danmakuCfgIndex));
        _entityManager.AddComponent(e_danmaku, new CPosition(posX, posY));
        _entityManager.AddComponent(e_danmaku, new CRotation(rotationRad));
        _entityManager.AddComponent(e_danmaku, new CVelocity(velX, velY));
        _entityManager.AddComponent(e_danmaku, new CCollider
        {
            isActive = true,
            shape = danmakuCfg.colliderConfig.shape,
            layer = danmakuCfg.colliderConfig.layer,
            mask = danmakuCfg.colliderConfig.mask,
            offsetX = danmakuCfg.colliderConfig.offset.x,
            offsetY = danmakuCfg.colliderConfig.offset.y,
            radius = danmakuCfg.colliderConfig.radius,
            width = danmakuCfg.colliderConfig.boxSize.x,
            height = danmakuCfg.colliderConfig.boxSize.y
        });

        if (danmakuCfg.danmakuType == E_DanmakuType.Homing)
        {
            int targetIndex = DanmakuHomingLogic.FindNearestTargetIndex(
                _entityManager, posX, posY, danmakuCfg.homingTargetLayerMask);

            float forwardX = velX;
            float forwardY = velY;
            if (forwardX * forwardX + forwardY * forwardY < 1e-8f)
            {
                forwardX = MathF.Cos(rotationRad);
                forwardY = MathF.Sin(rotationRad);
            }

            float speed = MathF.Sqrt(velX * velX + velY * velY);
            if (speed < 1e-8f)
            {
                speed = MathF.Sqrt(forwardX * forwardX + forwardY * forwardY);
                if (speed < 1e-8f)
                    speed = 1f;
            }

            sbyte curveBendSign = DanmakuHomingLogic.ResolveCurveBendSign(
                _entityManager, posX, posY, forwardX, forwardY, targetIndex, danmakuCfg.homingTargetLayerMask);

            _entityManager.AddComponent(e_danmaku, new CDanmakuHoming
            {
                targetEnemyIndex = targetIndex,
                speedPerFrame = speed,
                turnSpeedRadPerFrame = danmakuCfg.homingTurnSpeedRadPerFrame,
                homingTargetLayerMask = danmakuCfg.homingTargetLayerMask,
                curveBendSign = curveBendSign,
                outerArcActive = 1,
            });
        }

        return e_danmaku;
    }

    public Entity CreateEnemy(EnemyConfig enemyConfig, float posX, float posY, float hpMultiplier = 1f)
    {
        Entity e_enemy = _entityManager.CreateEntity();
        var enemyCfgIndex = GameResDB.Instance.GetConfigIndex(enemyConfig.ConfigId);
        int hp = Mathf.Max(1, Mathf.RoundToInt(enemyConfig.maxHealth * hpMultiplier));
        _entityManager.AddComponent(e_enemy, new CEnemy
        {
            enemyCfgIndex = enemyCfgIndex,
            currentHealth = hp,
            enemyType = (byte)enemyConfig.enemyType,
        });
        _entityManager.AddComponent(e_enemy, new CPosition(posX, posY));
        _entityManager.AddComponent(e_enemy, new CVelocity(0, 0));
        _entityManager.AddComponent(e_enemy, new CRotation(0));
        _entityManager.AddComponent(e_enemy, new CCollider
        {
            isActive = true,
            shape = enemyConfig.colliderConfig.shape,
            layer = enemyConfig.colliderConfig.layer,
            mask = enemyConfig.colliderConfig.mask,
            offsetX = enemyConfig.colliderConfig.offset.x,
            offsetY = enemyConfig.colliderConfig.offset.y,
            radius = enemyConfig.colliderConfig.radius,         
            width = enemyConfig.colliderConfig.boxSize.x,
            height = enemyConfig.colliderConfig.boxSize.y,
        });

        if (enemyConfig.emitterConfigIndex >= 0)
        {
            var emitterCfg = GameResDB.Instance.GetConfig<DanmakuEmitterConfig>(enemyConfig.emitterConfigIndex);
            if (emitterCfg != null)
            {
                var emitter = new CDanmakuEmitter(emitterCfg);
                emitter.randomSeed = (uint)((e_enemy.Index + 1) * 2246822519u);
                emitter.isEmitting = true;
                _entityManager.AddComponent(e_enemy, emitter);
            }
        }

        return e_enemy;
    }

    /// <summary>
    /// 生成掉落物 ECS 实体（竖直运动见 <see cref="CDropItemMotion"/>）；表现层需另行 <see cref="CPoolGetTag"/>。
    /// </summary>
    public Entity CreateDropItem(int dropCfgIndex, float posX, float posY)
        => CreateDropItem(dropCfgIndex, posX, posY, 0f, 0f, false);

    /// <param name="burstDirX">径向散开方向 X（仅 DirectionalBurstThenFall 生效）。</param>
    /// <param name="burstDirY">径向散开方向 Y。</param>
    public Entity CreateDropItem(
        int dropCfgIndex,
        float posX,
        float posY,
        float burstDirX,
        float burstDirY,
        bool useBurstDirectionOverride)
    {
        var cfg = GameResDB.Instance.GetConfig<DropItemConfig>(dropCfgIndex);
        if (cfg == null)
        {
            Logger.Error($"DropItemConfig not found for index {dropCfgIndex}.");
            return Entity.Null;
        }

        Entity e = _entityManager.CreateEntity();
        _entityManager.AddComponent(e, new CDropItem(dropCfgIndex));
        _entityManager.AddComponent(e, new CPosition(posX, posY));
        _entityManager.AddComponent(e, new CRotation(0));
        uint logicFps = GameManager.logicFPS > 0 ? (uint)GameManager.logicFPS : 60;
        _entityManager.AddComponent(
            e,
            DropItemMotionSimulator.CreateMotionFromConfig(
                cfg, logicFps, burstDirX, burstDirY, useBurstDirectionOverride));
        _entityManager.AddComponent(e, new CCollider
        {
            isActive = true,
            shape = cfg.colliderConfig.shape,
            layer = cfg.colliderConfig.layer,
            mask = cfg.colliderConfig.mask,
            offsetX = cfg.colliderConfig.offset.x,
            offsetY = cfg.colliderConfig.offset.y,
            radius = cfg.colliderConfig.radius,
            width = cfg.colliderConfig.boxSize.x,
            height = cfg.colliderConfig.boxSize.y,
        });
        return e;
    }
}
