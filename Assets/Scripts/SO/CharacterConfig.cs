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
    [Header("信息配置")]
    public E_Character characterID = E_Character.None;

    public E_Weapon[] weaponIds;

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
