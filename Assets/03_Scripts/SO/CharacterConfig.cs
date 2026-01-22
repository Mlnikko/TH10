using UnityEngine;
public enum E_Character : byte
{
    None = 0,
    Reimu = 1,
    Marisa = 2,
}

[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Configs/CharacterConfig")]
public class CharacterConfig : GameConfig
{
    [Header("ÐÅÏ¢ÅäÖÃ")]
    public E_Character CharacterID = E_Character.None;
    public E_Weapon[] AvailableWeapons;
    [TextArea(1, 5)]
    public string Description;

    [Header("ÒÆËÙÅäÖÃ")]
    public float MoveSpeed;
    public float MoveSlowSpeed;

    [Header("ÒÆ¶¯Åö×²ÌåÉèÖÃ")]
    public Vector2 MoveBoxSize = new(0.3f, 0.5f);
    public Vector2 MoveBoxOffset = new(0, 0.08f);

    [Header("ÊÜ»÷Åö×²ÌåÉèÖÃ")]
    public float HitRadius = 0.1f;

    [Header("²Áµ¯°ë¾¶")]
    public float GrazeRadius = 0.5f;

    public CPlayerAttribute ToRuntimeAttribute(float logicDeltaTime)
    {
        return new CPlayerAttribute
        {
            moveSpeedPerFrame = Mathf.Max(MoveSpeed, 0.01f) * logicDeltaTime,
            moveSlowSpeedPerFrame = Mathf.Max(MoveSlowSpeed, 0.01f) * logicDeltaTime,
            hitRadius = Mathf.Max(HitRadius, 0.01f),
            grazeRadius = Mathf.Max(GrazeRadius, 0.01f)
        };
    }

    public override string AddressableKeyPrefix => ConfigHelper.CHARACTER_CONFIG_PREFIX;
}
