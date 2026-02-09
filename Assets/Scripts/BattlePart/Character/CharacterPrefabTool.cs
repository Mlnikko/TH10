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

        characterName = characterConfig.characterName;
        description = characterConfig.description;
        speed = characterConfig.moveSpeed;
        slowSpeed = characterConfig.moveSlowSpeed;
        moveBoxSize = characterConfig.moveBoxSize;
        moveBoxOffset = characterConfig.moveBoxOffset;
        hitRadius = characterConfig.hitRadius;
        grazeRadius = characterConfig.grazeRadius;
    }

    public void SaveCharacterConfig()
    {
        // 此方法仅用于 Editor 保存，运行时调用无效！
        if (characterConfig == null) return;

        characterConfig.characterName = characterName;
        characterConfig.description = description;
        characterConfig.moveSpeed = speed;
        characterConfig.moveSlowSpeed = slowSpeed;
        characterConfig.moveBoxSize = moveBoxSize;
        characterConfig.moveBoxOffset = moveBoxOffset;
        characterConfig.hitRadius = hitRadius;
        characterConfig.grazeRadius = grazeRadius;
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
