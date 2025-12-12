using UnityEngine;
public enum E_Character
{
    None = 0,
    Reimu = 1,
    Marisa = 2,
}

[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Custom/characterConfig")]
public class CharacterConfig : GameConfig
{
    [Header("ÐÅÏ¢ÅäÖÃ")]
    public E_Character CharacterID;
    public E_Weapon[] AvailableWeapons;
    [TextArea(1, 5)]
    public string Description;

    [Header("ÒÆËÙÅäÖÃ")]
    public float MoveSpeed;
    public float MoveSlowSpeed;

    [Header("ÒÆ¶¯Åö×²ÌåÉèÖÃ")]
    public Vector2 MoveBoxSize;
    public Vector2 MoveBoxOffset;

    [Header("ÊÜ»÷Åö×²ÌåÉèÖÃ")]
    public float HitRadius;

    [Header("²Áµ¯°ë¾¶")]
    public float GrazeRadius;

    public CharacterConfig() 
    {
        CharacterID = E_Character.None;
        Description = "ÇëÊäÈë½ÇÉ«ÃèÊö";

        MoveBoxSize = new(0.3f, 0.5f);
        MoveBoxOffset = new(0, 0.08f);
        HitRadius = 0.1f;
        GrazeRadius = 0.5f;
    }

    public override string ConfigId => CharacterID.ToString();
}
