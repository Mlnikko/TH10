using System;
using UnityEngine;
public enum E_Character : byte
{
    None = 0,
    Character_Reimu = 1,
    Character_Marisa = 2,
}

[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Configs/CharacterConfig")]
public class CharacterConfig : GameConfig, IReferenceResolver
{
    [Header("预制体配置")]
    public string characterPrefabId;
    [NonSerialized]
    public int characterPrefabIndex = -1;

    [Header("信息配置")]
    public E_Character character = E_Character.None;

    [TextArea(1, 5)]
    public string description;

    [Header("生命配置")]
    public int maxHealth;

    [Header("移速配置")]
    public float moveSpeed;
    public float moveSlowSpeed;

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
    }
#endif

    public void ResolveReferences(GameResDB resDb)
    {
        // 1. 解析角色预制体索引
        characterPrefabIndex = resDb.GetPrefabIndex(characterPrefabId);
        if (characterPrefabIndex == -1)
        {
            Logger.Warn($"[CharacterConfig] Prefab not found for ID: '{characterPrefabId}' (configId: {ConfigId})", LogTag.Resource);
        }
    }
}
