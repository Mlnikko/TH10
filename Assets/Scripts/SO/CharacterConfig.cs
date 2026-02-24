using System;
using UnityEngine;
public enum E_Character : byte
{
    None = 0,
    Reimu = 1,
    Marisa = 2,
}

[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Configs/CharacterConfig")]
public class CharacterConfig : GameConfig, IReferenceResolver
{
    [Header("预制体配置")]
    public string characterPrefabId;
    [NonSerialized]
    public int characterPrefabIndex = -1;

    [Header("信息配置")]
    public E_Character characterName = E_Character.None;

    //public E_Weapon[] weaponIds;
    //[NonSerialized]
    //public int[] weaponIndices;

    [TextArea(1, 5)]
    public string description;

    [Header("移速配置")]
    public float moveSpeed;
    public float moveSlowSpeed;

    [Header("移动碰撞体设置")]
    public Vector2 moveBoxSize = new(0.3f, 0.5f);
    public Vector2 moveBoxOffset = new(0, 0.08f);

    [Header("受击碰撞体设置")]
    public float hitRadius = 0.1f;

    [Header("擦弹半径")]
    public float grazeRadius = 0.5f;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(characterPrefabId))
        {
            characterPrefabId = characterPrefabId.ToLowerInvariant().Trim();
        }
    }
#endif

    public void ResolveReferences(GameResDB resDb)
    {
        // 1. 解析角色预制体索引
        characterPrefabIndex = resDb.GetPrefabIndex(characterPrefabId);
        if (characterPrefabIndex == -1)
        {
            Logger.Warn($"[CharacterConfig] Prefab not found for ID: '{characterPrefabId}' (configId: {configId})", LogTag.Resource);
        }
    }

    public CPlayerAttribute ToRuntimeAttribute(float logicDeltaTime)
    {
        return new CPlayerAttribute
        {
            moveSpeedPerFrame = Mathf.Max(moveSpeed, 0.01f) * logicDeltaTime,
            moveSlowSpeedPerFrame = Mathf.Max(moveSlowSpeed, 0.01f) * logicDeltaTime,
            hitRadius = Mathf.Max(hitRadius, 0.01f),
            grazeRadius = Mathf.Max(grazeRadius, 0.01f)
        };
    }
}
