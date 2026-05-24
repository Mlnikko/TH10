using System;
using UnityEngine;

public enum E_Weapon : byte
{
    None = 0,

    Weapon_Reimu_Blue = 10,
    Weapon_Reimu_Red = 11,
    Weapon_Reimu_Pink = 12,

    Weapon_Marisa_0 = 20,
    Weapon_Marisa_1 = 21,
    Weapon_Marisa_2 = 22,
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
    [Tooltip("副发射器（双枪、僚机等），可配置多个偏移位")]
    public WeaponEmitterSlot[] slots;
}

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Configs/WeaponConfig")]
public class WeaponConfig : GameConfig, IReferenceResolver
{
    [Header("武器配置")]
    public E_Weapon weaponID;

    [Header("主发射器")]
    public WeaponPrimaryEmitterGroup primaryEmitters = new();

    [Header("副发射器")]
    public WeaponSecondaryEmitterGroup secondaryEmitters = new();

    [NonSerialized] public int primaryNormalEmitterCfgIndex = -1;
    [NonSerialized] public int primarySlowEmitterCfgIndex = -1;
    [NonSerialized] public int[] secondaryEmitterCfgIndices = Array.Empty<int>();

    [TextArea(1, 5)]
    public string description;

#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    string[] danmakuEmitterConfigIds;

    void OnValidate()
    {
        MigrateLegacyEmitterIds();
        NormalizeEmitterId(ref primaryEmitters.normal.danmakuEmitterConfigId);
        NormalizeEmitterId(ref primaryEmitters.slowModeDanmakuEmitterConfigId);

        if (secondaryEmitters.slots != null)
        {
            for (int i = 0; i < secondaryEmitters.slots.Length; i++)
            {
                var slot = secondaryEmitters.slots[i];
                NormalizeEmitterId(ref slot.danmakuEmitterConfigId);
                secondaryEmitters.slots[i] = slot;
            }
        }
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
#endif

    public int ResolvePrimaryEmitterCfgIndex(bool slowMode)
    {
        if (slowMode && primarySlowEmitterCfgIndex >= 0)
            return primarySlowEmitterCfgIndex;
        return primaryNormalEmitterCfgIndex;
    }

    public void ResolveReferences(GameResDB resDb)
    {
        primaryNormalEmitterCfgIndex = ResolveEmitterIndex(
            resDb,
            primaryEmitters.normal.danmakuEmitterConfigId,
            "primary.normal");

        string slowId = StringHelper.NormalizeResourceId(primaryEmitters.slowModeDanmakuEmitterConfigId);
        primarySlowEmitterCfgIndex = string.IsNullOrEmpty(slowId)
            ? -1
            : ResolveEmitterIndex(resDb, slowId, "primary.slowMode");

        var slots = secondaryEmitters.slots;
        if (slots != null && slots.Length > 0)
        {
            secondaryEmitterCfgIndices = new int[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                secondaryEmitterCfgIndices[i] = ResolveEmitterIndex(
                    resDb,
                    slots[i].danmakuEmitterConfigId,
                    $"secondary[{i}]");
            }
        }
        else
        {
            secondaryEmitterCfgIndices = Array.Empty<int>();
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
