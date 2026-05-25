using System;
using UnityEngine;

public enum E_Weapon : byte
{
    None = 0,

    Weapon_Reimu_Blue = 10,
    Weapon_Reimu_Red = 11,
    Weapon_Reimu_Pink = 12,

    Weapon_Marisa_Green = 20,
    Weapon_Marisa_Red = 21,
    Weapon_Marisa_Blue = 22,
}

public enum E_WeaponEmitterSlotRole : byte
{
    Primary = 0,
    Secondary = 1,
}

[Serializable]
public struct WeaponEmitterSlot
{
    [Tooltip("DanmakuEmitterConfig 的 ConfigId")]
    [DanmakuEmitterConfigId]
    public string danmakuEmitterConfigId;

    [Tooltip("相对玩家位置的额外发射点偏移（叠加 DanmakuEmitterConfig.emitterPosOffset）")]
    public Vector2 slotOffset;
}

[Serializable]
public class WeaponPrimaryEmitterGroup
{
    [Tooltip("通常模式主发射器")]
    public WeaponEmitterSlot normal;

    [Tooltip("低速/聚焦模式主发射器 ConfigId；为空则沿用 normal")]
    [DanmakuEmitterConfigId]
    public string slowModeDanmakuEmitterConfigId;
}

[Serializable]
public class WeaponSecondaryEmitterGroup
{
    [Tooltip("副发射器（双枪、僚机等），可配置多个偏移位；仅作迁移源，运行时使用 powerSecondaryLayouts")]
    public WeaponEmitterSlot[] slots;
}

[Serializable]
public class WeaponPowerSecondaryLayout
{
    [Min(0)]
    [Tooltip("玩家 powerOrbs >= 此值时切换到本档（整组替换，不在低档副炮上叠加）")]
    public int minPowerOrbs;

    [Tooltip("本档副炮布局（该档的完整发射点列表，非增量）")]
    public WeaponEmitterSlot[] slots;
}

/// <summary>低速模式下主炮按 Power 分档（整档替换发射器与槽位偏移，非叠加）。</summary>
[Serializable]
public class WeaponPowerPrimarySlowLayout
{
    [Min(0)]
    [Tooltip("玩家 powerOrbs >= 此值时在低速模式下使用本档主炮")]
    public int minPowerOrbs;

    [Tooltip("本档主炮槽位（发射器 ConfigId + 偏移）")]
    public WeaponEmitterSlot slot;
}

/// <summary>烘焙后的 Power 副炮档（<see cref="WeaponConfig.ResolveReferences"/> 填充）。</summary>
public struct WeaponPowerSecondaryResolved
{
    public int minPowerOrbs;
    public int[] emitterCfgIndices;
    public WeaponEmitterSlot[] slots;
}

/// <summary>烘焙后的低速主炮 Power 档。</summary>
public struct WeaponPowerPrimarySlowResolved
{
    public int minPowerOrbs;
    public int emitterCfgIndex;
    public WeaponEmitterSlot slot;
}

[Serializable]
public class WeaponDisplayConfig
{
    [Tooltip("准备界面显示名；为空则用 description 或 ConfigId")]
    public string selectionName;

    [TextArea(1, 4)]
    public string description;
}

[Serializable]
public class WeaponSlowModeLayoutConfig
{
    [Tooltip("低速时主发射器槽位向玩家中心收束比例（0=不变，1=缩到原点）")]
    [Range(0f, 1f)]
    public float primarySlotConverge;

    [Tooltip("低速时副发射器槽位向玩家中心收束比例（0=不变，1=缩到原点）")]
    [Range(0f, 1f)]
    public float secondarySlotConverge = 1f;

    [Tooltip("副炮收束/展开速度（0~1 每秒；0 表示瞬时切换）")]
    [Min(0f)]
    public float secondarySlotConvergeSpeed = 4f;
}

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Configs/WeaponConfig")]
public class WeaponConfig : GameConfig, IReferenceResolver
{
    [Header("武器配置")]
    public E_Weapon weaponID;

    [Header("预制体")]
    [Tooltip("战斗表现用武器预制体 id；为空时默认与 ConfigId 相同（如 weapon_reimu_blue）")]
    public string weaponPrefabId;

    [NonSerialized] public int weaponPrefabIndex = -1;

    [Header("显示")]
    public WeaponDisplayConfig display = new();

    [Header("主发射器")]
    public WeaponPrimaryEmitterGroup primaryEmitters = new();

    [Header("低速主炮（按 Power）")]
    [Tooltip("低速/聚焦模式下按 minPowerOrbs 切换主炮；为空则回退 primaryEmitters.slowMode 或 normal")]
    public WeaponPowerPrimarySlowLayout[] powerPrimarySlowLayouts = Array.Empty<WeaponPowerPrimarySlowLayout>();

    [Header("副发射器（按 Power）")]
    [Tooltip("按 minPowerOrbs 分档；拾取 P 后切换到满足 powerOrbs 的最高档（整档替换副炮，非叠加）")]
    public WeaponPowerSecondaryLayout[] powerSecondaryLayouts = Array.Empty<WeaponPowerSecondaryLayout>();

    [Header("副发射器（旧字段，自动迁移）")]
    [HideInInspector]
    public WeaponSecondaryEmitterGroup secondaryEmitters = new();

    [Header("低速收束布局")]
    public WeaponSlowModeLayoutConfig slowModeLayout = new();

    [NonSerialized] public int primaryNormalEmitterCfgIndex = -1;
    [NonSerialized] public int primarySlowEmitterCfgIndex = -1;
    [NonSerialized] public WeaponPowerPrimarySlowResolved[] powerPrimarySlowResolved = Array.Empty<WeaponPowerPrimarySlowResolved>();
    [NonSerialized] public WeaponPowerSecondaryResolved[] powerSecondaryResolved = Array.Empty<WeaponPowerSecondaryResolved>();

    [TextArea(1, 5)]
    public string description;

    public string GetSelectionDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(display.selectionName))
            return display.selectionName;
        if (!string.IsNullOrWhiteSpace(display.description))
            return display.description;
        if (!string.IsNullOrWhiteSpace(description))
            return description;
        return ConfigId;
    }

    public Vector2 ResolvePrimarySlotOffset(bool slowMode, int powerOrbs = 0)
    {
        float converge = slowMode ? slowModeLayout.primarySlotConverge : 0f;
        Vector2 baseOffset = primaryEmitters.normal.slotOffset;
        if (slowMode && TryResolvePowerPrimarySlow(powerOrbs, out var tier))
            baseOffset = tier.slot.slotOffset;
        return baseOffset * (1f - converge);
    }

    public Vector2 ResolveSecondarySlotOffset(Vector2 baseOffset, float converge01) =>
        baseOffset * (1f - slowModeLayout.secondarySlotConverge * Mathf.Clamp01(converge01));

    public Vector2 ResolveSecondarySlotOffset(Vector2 baseOffset, bool slowMode) =>
        ResolveSecondarySlotOffset(baseOffset, slowMode ? 1f : 0f);

    /// <summary>根据当前火力选取低速主炮烘焙档。</summary>
    public bool TryResolvePowerPrimarySlow(int powerOrbs, out WeaponPowerPrimarySlowResolved resolved)
    {
        resolved = default;
        var tiers = powerPrimarySlowResolved;
        if (tiers == null || tiers.Length == 0)
            return false;

        bool found = false;
        for (int i = 0; i < tiers.Length; i++)
        {
            ref readonly var tier = ref tiers[i];
            if (tier.minPowerOrbs > powerOrbs || tier.emitterCfgIndex < 0)
                continue;

            if (!found || tier.minPowerOrbs >= resolved.minPowerOrbs)
            {
                resolved = tier;
                found = true;
            }
        }

        return found;
    }

    /// <summary>编辑器未烘焙或布局预览时解析低速主炮槽位。</summary>
    public bool TryGetPrimarySlowSlotForPower(int powerOrbs, out WeaponEmitterSlot slot)
    {
        slot = default;
        if (TryResolvePowerPrimarySlow(powerOrbs, out var resolved) && resolved.emitterCfgIndex >= 0)
        {
            slot = resolved.slot;
            return true;
        }

        if (powerPrimarySlowLayouts != null && powerPrimarySlowLayouts.Length > 0)
            return TryPickPrimarySlowSlotFromLayouts(powerPrimarySlowLayouts, powerOrbs, out slot);

        string legacyId = primaryEmitters.slowModeDanmakuEmitterConfigId;
        if (!string.IsNullOrWhiteSpace(legacyId))
        {
            slot = new WeaponEmitterSlot
            {
                danmakuEmitterConfigId = legacyId,
                slotOffset = primaryEmitters.normal.slotOffset,
            };
            return true;
        }

        return false;
    }

    static bool TryPickPrimarySlowSlotFromLayouts(
        WeaponPowerPrimarySlowLayout[] layouts,
        int powerOrbs,
        out WeaponEmitterSlot slot)
    {
        slot = default;
        if (layouts == null || layouts.Length == 0)
            return false;

        WeaponPowerPrimarySlowLayout best = null;
        for (int i = 0; i < layouts.Length; i++)
        {
            var layout = layouts[i];
            if (layout == null || layout.minPowerOrbs > powerOrbs)
                continue;
            if (string.IsNullOrWhiteSpace(layout.slot.danmakuEmitterConfigId))
                continue;
            if (best == null || layout.minPowerOrbs >= best.minPowerOrbs)
                best = layout;
        }

        if (best == null)
            return false;

        slot = best.slot;
        return true;
    }

    /// <summary>根据当前火力选取应启用的副炮烘焙档。</summary>
    public bool TryResolvePowerSecondary(int powerOrbs, out WeaponPowerSecondaryResolved resolved)
    {
        resolved = default;
        var tiers = powerSecondaryResolved;
        if (tiers == null || tiers.Length == 0)
            return false;

        bool found = false;

        for (int i = 0; i < tiers.Length; i++)
        {
            ref readonly var tier = ref tiers[i];
            if (tier.minPowerOrbs > powerOrbs)
                continue;

            if (!found || tier.minPowerOrbs >= resolved.minPowerOrbs)
            {
                resolved = tier;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// 获取指定 Power 下副炮槽位。运行时优先烘焙表；编辑器未烘焙时回退到序列化 <see cref="powerSecondaryLayouts"/>。
    /// </summary>
    public bool TryGetSecondarySlotsForPower(int powerOrbs, out WeaponEmitterSlot[] slots)
    {
        slots = null;

        if (TryResolvePowerSecondary(powerOrbs, out var resolved) &&
            resolved.slots != null &&
            resolved.slots.Length > 0)
        {
            slots = resolved.slots;
            return true;
        }

        if (powerSecondaryLayouts != null && powerSecondaryLayouts.Length > 0)
            return TryPickSecondarySlotsFromLayouts(powerSecondaryLayouts, powerOrbs, out slots);

        var legacy = secondaryEmitters?.slots;
        if (legacy != null && legacy.Length > 0)
        {
            slots = legacy;
            return true;
        }

        return false;
    }

    static bool TryPickSecondarySlotsFromLayouts(
        WeaponPowerSecondaryLayout[] layouts,
        int powerOrbs,
        out WeaponEmitterSlot[] slots)
    {
        slots = null;
        if (layouts == null || layouts.Length == 0)
            return false;

        WeaponPowerSecondaryLayout best = null;
        for (int i = 0; i < layouts.Length; i++)
        {
            var layout = layouts[i];
            if (layout?.slots == null || layout.slots.Length == 0)
                continue;
            if (layout.minPowerOrbs > powerOrbs)
                continue;
            if (best == null || layout.minPowerOrbs >= best.minPowerOrbs)
                best = layout;
        }

        if (best == null)
            return false;

        slots = best.slots;
        return true;
    }

    public bool HasAnyPowerSecondaryLayout()
    {
        var tiers = powerSecondaryResolved;
        if (tiers == null || tiers.Length == 0)
            return false;

        for (int i = 0; i < tiers.Length; i++)
        {
            if (tiers[i].slots != null && tiers[i].slots.Length > 0)
                return true;
        }

        return false;
    }

    void MigrateLegacyPrimarySlowToPowerLayouts()
    {
        if (powerPrimarySlowLayouts != null && powerPrimarySlowLayouts.Length > 0)
            return;

        string slowId = primaryEmitters.slowModeDanmakuEmitterConfigId;
        if (string.IsNullOrWhiteSpace(slowId))
            return;

        powerPrimarySlowLayouts = new[]
        {
            new WeaponPowerPrimarySlowLayout
            {
                minPowerOrbs = 0,
                slot = new WeaponEmitterSlot
                {
                    danmakuEmitterConfigId = slowId,
                    slotOffset = primaryEmitters.normal.slotOffset,
                },
            },
        };
    }

    void MigrateLegacySecondaryToPowerLayouts()
    {
        if (powerSecondaryLayouts != null && powerSecondaryLayouts.Length > 0)
            return;

        var legacy = secondaryEmitters?.slots;
        if (legacy == null || legacy.Length == 0)
            return;

        powerSecondaryLayouts = new[]
        {
            new WeaponPowerSecondaryLayout
            {
                minPowerOrbs = 0,
                slots = legacy,
            },
        };
    }

#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    string[] danmakuEmitterConfigIds;

    void OnValidate()
    {
        MigrateLegacyEmitterIds();
        MigrateLegacySecondaryToPowerLayouts();
        MigrateLegacyPrimarySlowToPowerLayouts();
        SyncDisplayFromLegacyDescription();
        NormalizeEmitterId(ref primaryEmitters.normal.danmakuEmitterConfigId);
        NormalizeEmitterId(ref primaryEmitters.slowModeDanmakuEmitterConfigId);

        if (powerPrimarySlowLayouts != null)
        {
            for (int t = 0; t < powerPrimarySlowLayouts.Length; t++)
            {
                var layout = powerPrimarySlowLayouts[t];
                if (layout == null)
                    continue;
                NormalizeEmitterId(ref layout.slot.danmakuEmitterConfigId);
                powerPrimarySlowLayouts[t] = layout;
            }
        }

        if (secondaryEmitters.slots != null)
        {
            for (int i = 0; i < secondaryEmitters.slots.Length; i++)
            {
                var slot = secondaryEmitters.slots[i];
                NormalizeEmitterId(ref slot.danmakuEmitterConfigId);
                secondaryEmitters.slots[i] = slot;
            }
        }

        if (powerSecondaryLayouts != null)
        {
            for (int t = 0; t < powerSecondaryLayouts.Length; t++)
            {
                var layout = powerSecondaryLayouts[t];
                if (layout?.slots == null)
                    continue;

                for (int i = 0; i < layout.slots.Length; i++)
                {
                    var slot = layout.slots[i];
                    NormalizeEmitterId(ref slot.danmakuEmitterConfigId);
                    layout.slots[i] = slot;
                }
            }
        }

        weaponPrefabId = weaponPrefabId.ToLowerInvariantTrimmed();
        if (string.IsNullOrEmpty(weaponPrefabId))
            weaponPrefabId = ConfigId;

        ClearLegacySecondaryWhenPowerLayoutsExist();
        ClearLegacyPrimarySlowIdWhenPowerLayoutsExist();
    }

    void ClearLegacyPrimarySlowIdWhenPowerLayoutsExist()
    {
        if (powerPrimarySlowLayouts == null || powerPrimarySlowLayouts.Length == 0)
            return;

        if (string.IsNullOrWhiteSpace(primaryEmitters.slowModeDanmakuEmitterConfigId))
            return;

        primaryEmitters.slowModeDanmakuEmitterConfigId = string.Empty;
    }

    void ClearLegacySecondaryWhenPowerLayoutsExist()
    {
        if (powerSecondaryLayouts == null || powerSecondaryLayouts.Length == 0)
            return;

        if (secondaryEmitters?.slots == null || secondaryEmitters.slots.Length == 0)
            return;

        secondaryEmitters.slots = Array.Empty<WeaponEmitterSlot>();
    }

    void MigrateLegacyEmitterIds()
    {
        if (danmakuEmitterConfigIds == null || danmakuEmitterConfigIds.Length == 0)
            return;

        if (!string.IsNullOrEmpty(primaryEmitters.normal.danmakuEmitterConfigId))
        {
            danmakuEmitterConfigIds = null;
            return;
        }

        primaryEmitters.normal.danmakuEmitterConfigId = danmakuEmitterConfigIds[0];
        if (danmakuEmitterConfigIds.Length > 1)
        {
            var migrated = new WeaponEmitterSlot[danmakuEmitterConfigIds.Length - 1];
            for (int i = 1; i < danmakuEmitterConfigIds.Length; i++)
            {
                migrated[i - 1] = new WeaponEmitterSlot
                {
                    danmakuEmitterConfigId = danmakuEmitterConfigIds[i],
                };
            }

            secondaryEmitters.slots = migrated;
        }

        danmakuEmitterConfigIds = null;
    }

    static void NormalizeEmitterId(ref string id)
    {
        if (!string.IsNullOrEmpty(id))
            id = StringHelper.NormalizeResourceId(id);
    }

    void SyncDisplayFromLegacyDescription()
    {
        if (display == null)
            display = new WeaponDisplayConfig();

        if (string.IsNullOrWhiteSpace(display.description) && !string.IsNullOrWhiteSpace(description))
            display.description = description;
    }
#endif

    public int ResolvePrimaryEmitterCfgIndex(bool slowMode, int powerOrbs = 0)
    {
        if (!slowMode)
            return primaryNormalEmitterCfgIndex;

        if (TryResolvePowerPrimarySlow(powerOrbs, out var tier) && tier.emitterCfgIndex >= 0)
            return tier.emitterCfgIndex;

        if (primarySlowEmitterCfgIndex >= 0)
            return primarySlowEmitterCfgIndex;

        return primaryNormalEmitterCfgIndex;
    }

    public void ResolveReferences(GameResDB resDb)
    {
        string prefabId = StringHelper.NormalizeResourceId(
            string.IsNullOrEmpty(weaponPrefabId) ? ConfigId : weaponPrefabId);
        weaponPrefabIndex = resDb.GetPrefabIndex(prefabId);
        if (weaponPrefabIndex == -1)
        {
            Logger.Warn(
                $"[WeaponConfig] Weapon prefab not found: '{prefabId}' (config: {ConfigId})",
                LogTag.Resource);
        }

        primaryNormalEmitterCfgIndex = ResolveEmitterIndex(
            resDb,
            primaryEmitters.normal.danmakuEmitterConfigId,
            "primary.normal");

        MigrateLegacyPrimarySlowToPowerLayouts();

        string slowId = StringHelper.NormalizeResourceId(primaryEmitters.slowModeDanmakuEmitterConfigId);
        primarySlowEmitterCfgIndex = string.IsNullOrEmpty(slowId)
            ? -1
            : ResolveEmitterIndex(resDb, slowId, "primary.slowMode");

        if (powerPrimarySlowLayouts != null && powerPrimarySlowLayouts.Length > 0)
        {
            powerPrimarySlowResolved = new WeaponPowerPrimarySlowResolved[powerPrimarySlowLayouts.Length];
            for (int t = 0; t < powerPrimarySlowLayouts.Length; t++)
            {
                var layout = powerPrimarySlowLayouts[t];
                if (layout == null || string.IsNullOrWhiteSpace(layout.slot.danmakuEmitterConfigId))
                {
                    powerPrimarySlowResolved[t] = new WeaponPowerPrimarySlowResolved
                    {
                        minPowerOrbs = layout?.minPowerOrbs ?? 0,
                        emitterCfgIndex = -1,
                        slot = layout?.slot ?? default,
                    };
                    continue;
                }

                int idx = ResolveEmitterIndex(
                    resDb,
                    layout.slot.danmakuEmitterConfigId,
                    $"power[{layout.minPowerOrbs}].primarySlow");

                powerPrimarySlowResolved[t] = new WeaponPowerPrimarySlowResolved
                {
                    minPowerOrbs = layout.minPowerOrbs,
                    emitterCfgIndex = idx,
                    slot = layout.slot,
                };
            }
        }
        else
            powerPrimarySlowResolved = Array.Empty<WeaponPowerPrimarySlowResolved>();

        MigrateLegacySecondaryToPowerLayouts();

        if (powerSecondaryLayouts != null && powerSecondaryLayouts.Length > 0)
        {
            powerSecondaryResolved = new WeaponPowerSecondaryResolved[powerSecondaryLayouts.Length];
            for (int t = 0; t < powerSecondaryLayouts.Length; t++)
            {
                var layout = powerSecondaryLayouts[t];
                var slots = layout?.slots;
                if (slots == null || slots.Length == 0)
                {
                    powerSecondaryResolved[t] = new WeaponPowerSecondaryResolved
                    {
                        minPowerOrbs = layout?.minPowerOrbs ?? 0,
                        emitterCfgIndices = Array.Empty<int>(),
                        slots = Array.Empty<WeaponEmitterSlot>(),
                    };
                    continue;
                }

                var indices = new int[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    indices[i] = ResolveEmitterIndex(
                        resDb,
                        slots[i].danmakuEmitterConfigId,
                        $"power[{layout.minPowerOrbs}].secondary[{i}]");
                }

                powerSecondaryResolved[t] = new WeaponPowerSecondaryResolved
                {
                    minPowerOrbs = layout.minPowerOrbs,
                    emitterCfgIndices = indices,
                    slots = slots,
                };
            }
        }
        else
        {
            powerSecondaryResolved = Array.Empty<WeaponPowerSecondaryResolved>();
        }
    }

    int ResolveEmitterIndex(GameResDB resDb, string emitterId, string slotLabel)
    {
        emitterId = StringHelper.NormalizeResourceId(emitterId);
        if (string.IsNullOrEmpty(emitterId))
            return -1;

        int index = resDb.GetConfigIndex(emitterId);
        if (index == -1)
        {
            Logger.Warn(
                $"[WeaponConfig] DanmakuEmitter config not found: '{emitterId}' " +
                $"(slot: {slotLabel}, weapon: {ConfigId})",
                LogTag.Resource);
        }

        return index;
    }
}
