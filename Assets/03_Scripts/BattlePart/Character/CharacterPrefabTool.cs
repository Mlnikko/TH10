using UnityEngine;

public class CharacterPrefabTool : MonoBehaviour
{
    public CharacterConfig CharacterConfig => characterConfig;

    [SerializeField] CharacterConfig characterConfig;

    [Header("信息配置")]
    [SerializeField] E_Character characterName;

    [TextArea(1, 5)]
    [SerializeField] string description;

    [Header("移速配置")]
    [SerializeField] float speed;
    [SerializeField] float slowSpeed;

    [Header("移动碰撞体设置")]
    [SerializeField] Vector2 moveBoxSize;
    [SerializeField] Vector2 moveBoxOffset;

    [Header("受击碰撞体设置")]
    [SerializeField] float hitRadius;

    [Header("擦弹碰撞体设置")]
    [SerializeField] float grazeRadius;

    void Awake()
    {
        LoadCharacterConfig();
    }

    public void LoadCharacterConfig()
    {
        if(characterConfig == null) return;

        characterName = characterConfig.CharacterID;
        description = characterConfig.Description;
        speed = characterConfig.MoveSpeed;
        slowSpeed = characterConfig.MoveSlowSpeed;
        moveBoxSize = characterConfig.MoveBoxSize;
        moveBoxOffset = characterConfig.MoveBoxOffset;
        hitRadius = characterConfig.HitRadius;
        grazeRadius = characterConfig.GrazeRadius;
    }

    public void SaveCharacterConfig()
    {
        // 此方法仅用于 Editor 保存，运行时调用无效！
        if (characterConfig == null) return;

        characterConfig.CharacterID = characterName;
        characterConfig.Description = description;
        characterConfig.MoveSpeed = speed;
        characterConfig.MoveSlowSpeed = slowSpeed;
        characterConfig.MoveBoxSize = moveBoxSize;
        characterConfig.MoveBoxOffset = moveBoxOffset;
        characterConfig.HitRadius = hitRadius;
        characterConfig.GrazeRadius = grazeRadius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)moveBoxOffset, moveBoxSize);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, hitRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, grazeRadius);
    }
}
