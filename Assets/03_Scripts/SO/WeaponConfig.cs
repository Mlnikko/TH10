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
public class WeaponConfig : GameConfig
{
    [Header("ÎäÆ÷ÅäÖÃ")]
    public E_Weapon WeaponID;
    public string Name;

    public int[] DanmakuEmitterConfigIds;
    [TextArea(1, 5)]
    public string Description;

    public override string AddressableKeyPrefix => ConfigHelper.WEAPON_CONFIG_PREFIX;
}
