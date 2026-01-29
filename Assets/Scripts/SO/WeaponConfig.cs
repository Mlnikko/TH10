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
    [Header("Œ‰∆˜≈‰÷√")]
    public E_Weapon weaponID;

    public string[] danmakuEmitterConfigIds;

    [TextArea(1, 5)]
    public string description;
}
