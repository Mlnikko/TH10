using System;
using UnityEngine;

public enum E_Weapon
{
    Weapon_Reimu_0,
    Weapon_Reimu_1,
    Weapon_Reimu_2,

    Weapon_Marisa_0,
    Weapon_Marisa_1,
    Weapon_Marisa_2
}

[Serializable]
public class Weapon
{
    public string Name;
    public E_Weapon weapon;
    [TextArea(1, 5)]
    public string Description;
}

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Custom/WeaponConfig")]
public class WeaponConfig : ScriptableObject
{
    public Weapon[] weapons;
}
