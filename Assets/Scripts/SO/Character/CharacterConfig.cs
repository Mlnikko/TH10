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
    public int maxHealth;

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
    }

    public void BakeLogicTiming(uint logicFPS)
    {
        moveDistancePerFrame = moveSpeed / logicFPS;
        moveSlowDistancePerFrame = moveSlowSpeed / logicFPS;
    }
}
