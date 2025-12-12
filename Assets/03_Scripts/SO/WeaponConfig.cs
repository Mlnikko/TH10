using UnityEngine;

public enum E_Weapon
{
    None = 0,

    Weapon_Reimu_0 = 1000,
    Weapon_Reimu_1 = 1001,
    Weapon_Reimu_2 = 1002,

    Weapon_Marisa_0 = 1010,
    Weapon_Marisa_1 = 1011,
    Weapon_Marisa_2 = 1012,
}

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Custom/WeaponConfig")]
public class WeaponConfig : GameConfig
{
    public string Name;
    public E_Weapon WeaponID;
    [TextArea(1, 5)]
    public string Description;

    public override string ConfigId => WeaponID.ToString();
}
