using UnityEngine;
public enum E_CharacterName
{
    None,
    Reimu,
    Marisa,
    Rin
}

[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Custom/characterConfig")]
public class CharacterConfig : ScriptableObject
{
    [Header("信息配置")]
    public E_CharacterName CharacterName;
    [TextArea(1, 5)]
    public string Description;

    [Header("移速配置")]
    public float Speed;
    public float SlowSpeed;

    [Header("移动碰撞体设置")]
    public Vector2 MoveBoxSize;
    public Vector2 MoveBoxOffset;

    [Header("受击碰撞体设置")]
    public float HitRadius;

    [Header("擦弹半径")]
    public float GrazeRadius;

    public CharacterConfig() 
    {
        CharacterName = E_CharacterName.None;
        Description = "请输入角色描述";

        MoveBoxSize = new(0.3f, 0.5f);
        MoveBoxOffset = new(0, 0.08f);
        HitRadius = 0.1f;
        GrazeRadius = 0.5f;
    }
}
