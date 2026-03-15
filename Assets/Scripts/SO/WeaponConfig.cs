using System;
using UnityEngine;

public enum E_Weapon : byte
{
    None = 0,

    Weapon_Reimu_0 = 10,
    Weapon_Reimu_1 = 11,
    Weapon_Reimu_2 = 12,

    Weapon_Marisa_0 = 20,
    Weapon_Marisa_1 = 21,
    Weapon_Marisa_2 = 22,
}

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Configs/WeaponConfig")]
public class WeaponConfig : GameConfig , IReferenceResolver
{
    [Header("武器配置")]
    public E_Character characterID;

    public E_Weapon weaponID;

    [Header("武器发射器配置")]
    public string[] danmakuEmitterConfigIds;
    [NonSerialized]
    public int[] danmakuEmitterCfgIndices;

    [TextArea(1, 5)]
    public string description;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (danmakuEmitterConfigIds != null)
        {
            for (int i = 0; i < danmakuEmitterConfigIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(danmakuEmitterConfigIds[i]))
                {
                    danmakuEmitterConfigIds[i] = danmakuEmitterConfigIds[i].ToLowerInvariant().Trim();
                }
            }
        }
    }
#endif

    public void ResolveReferences(GameResDB resDb)
    {
        if (danmakuEmitterConfigIds != null && danmakuEmitterConfigIds.Length > 0)
        {
            danmakuEmitterCfgIndices = new int[danmakuEmitterConfigIds.Length];
            for (int i = 0; i < danmakuEmitterConfigIds.Length; i++)
            {
                string emitterId = danmakuEmitterConfigIds[i].ToLowerInvariant();
                danmakuEmitterCfgIndices[i] = resDb.GetConfigIndex(emitterId);

                if (danmakuEmitterCfgIndices[i] == -1)
                {
                    Logger.Warn(
                        $"[WeaponConfig] DanmakuEmitter config not found: '{emitterId}' " +
                        $"(in weapon: {configId})",
                        LogTag.Resource
                    );
                }
            }
        }
        else
        {
            danmakuEmitterCfgIndices = Array.Empty<int>();
        }
    }
}
