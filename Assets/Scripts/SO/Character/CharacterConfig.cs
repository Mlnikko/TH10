using System;
using UnityEngine;
public enum E_Character : byte
{
    None = 0,
    Character_Reimu = 1,
    Character_Marisa = 2,
}

[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Configs/CharacterConfig")]
public class CharacterConfig : GameConfig, IReferenceResolver, ILogicTimingBake
{
    [Header("预制体配置")]
    public string characterPrefabId;
    [NonSerialized]
    public int characterPrefabIndex = -1;

    [Header("信息配置")]
    public E_Character character = E_Character.None;

    [Header("可选武器")]
    [Tooltip("该角色可选的 WeaponConfig ConfigId 列表")]
    public string[] weaponConfigIds = Array.Empty<string>();

    [NonSerialized]
    public int[] weaponCfgIndices = Array.Empty<int>();

    [TextArea(1, 5)]
    public string description;

    [Header("生命配置")]
    public int maxHealth = 5;

    [Tooltip("受击摧毁后至复活的等待（秒）")]
    public float deathRespawnDelay = 0.5f;
    [NonSerialized]
    public int deathRespawnDelayFrames;

    [Tooltip("受击复活后的无敌时间（秒）")]
    public float postHitInvincibleDuration = 2f;
    [NonSerialized]
    public int postHitInvincibleFrames;

    [Tooltip("死亡时散落的 Power 道具 ConfigId")]
    public string deathPowerDropConfigId = "drop_rpr_s";
    [NonSerialized]
    public int deathPowerDropCfgIndex = -1;

    [Header("移速配置")]
    [Tooltip("移动速度（世界单位/秒）；烘焙为 moveDistancePerFrame")]
    public float moveSpeed;
    [NonSerialized]
    public float moveDistancePerFrame;

    [Tooltip("低速模式移动速度（世界单位/秒）；烘焙为 moveSlowDistancePerFrame")]
    public float moveSlowSpeed;
    [NonSerialized]
    public float moveSlowDistancePerFrame;

    [Header("移动碰撞体设置")]
    public ColliderConfig moveColliderConfig;

    [Header("受击碰撞体设置")]
    public ColliderConfig hitColliderConfig;

    [Header("擦弹半径")]
    public ColliderConfig grazeColliderConfig;

#if UNITY_EDITOR
    void OnValidate()
    {
        characterPrefabId = characterPrefabId.ToLowerInvariantTrimmed();
        deathPowerDropConfigId = StringHelper.NormalizeResourceId(deathPowerDropConfigId);
        NormalizeWeaponConfigIds();
    }

    static void NormalizeWeaponConfigIds(ref string[] ids)
    {
        if (ids == null)
            return;

        for (int i = 0; i < ids.Length; i++)
        {
            if (!string.IsNullOrEmpty(ids[i]))
                ids[i] = StringHelper.NormalizeResourceId(ids[i]);
        }
    }

    void NormalizeWeaponConfigIds()
    {
        NormalizeWeaponConfigIds(ref weaponConfigIds);
    }
#endif

    public void ResolveReferences(GameResDB resDb)
    {
        characterPrefabIndex = resDb.GetPrefabIndex(characterPrefabId);
        if (characterPrefabIndex == -1)
        {
            Logger.Warn($"[CharacterConfig] Prefab not found for ID: '{characterPrefabId}' (configId: {ConfigId})", LogTag.Resource);
        }

        if (weaponConfigIds != null && weaponConfigIds.Length > 0)
        {
            weaponCfgIndices = new int[weaponConfigIds.Length];
            for (int i = 0; i < weaponConfigIds.Length; i++)
            {
                string weaponId = StringHelper.NormalizeResourceId(weaponConfigIds[i]);
                weaponCfgIndices[i] = resDb.GetConfigIndex(weaponId);
                if (weaponCfgIndices[i] == -1)
                {
                    Logger.Warn(
                        $"[CharacterConfig] WeaponConfig not found: '{weaponId}' (character: {ConfigId})",
                        LogTag.Resource);
                }
            }
        }
        else
        {
            weaponCfgIndices = Array.Empty<int>();
        }

        string dropId = StringHelper.NormalizeResourceId(deathPowerDropConfigId);
        deathPowerDropCfgIndex = string.IsNullOrEmpty(dropId) ? -1 : resDb.GetConfigIndex(dropId);
        if (!string.IsNullOrEmpty(dropId) && deathPowerDropCfgIndex < 0)
        {
            Logger.Warn(
                $"[CharacterConfig] DropItemConfig not found: '{dropId}' (character: {ConfigId})",
                LogTag.Resource);
        }
    }

    public void BakeLogicTiming(uint logicFPS)
    {
        moveDistancePerFrame = moveSpeed / logicFPS;
        moveSlowDistancePerFrame = moveSlowSpeed / logicFPS;

        deathRespawnDelayFrames = Mathf.Max(1, Mathf.RoundToInt(deathRespawnDelay * logicFPS));
        postHitInvincibleFrames = Mathf.Max(1, Mathf.RoundToInt(postHitInvincibleDuration * logicFPS));
    }
}
