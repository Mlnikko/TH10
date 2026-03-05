using UnityEngine;

public class CharacterConfigViewer : MonoBehaviour
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

    [Header("移动碰撞体配置")]
    [SerializeField] ColliderConfig moveColliderConfig;

    [Header("受击碰撞体配置")]
    [SerializeField] ColliderConfig hitColliderConfig;

    [Header("擦弹碰撞体配置")]
    [SerializeField] ColliderConfig grazeColliderConfig;

    void Awake()
    {
        LoadCharacterConfig();
    }

    public void LoadCharacterConfig()
    {
        if(characterConfig == null) return;

        characterName = characterConfig.character;
        description = characterConfig.description;
        speed = characterConfig.moveSpeed;
        slowSpeed = characterConfig.moveSlowSpeed;

        moveColliderConfig = characterConfig.moveColliderConfig;
        hitColliderConfig = characterConfig.hitColliderConfig;
        grazeColliderConfig = characterConfig.grazeColliderConfig;
    }

    public void SaveCharacterConfig()
    {
        // 此方法仅用于 Editor 保存，运行时调用无效！
        if (characterConfig == null) return;

        characterConfig.character = characterName;
        characterConfig.description = description;
        characterConfig.moveSpeed = speed;
        characterConfig.moveSlowSpeed = slowSpeed;

        characterConfig.moveColliderConfig = moveColliderConfig;
        characterConfig.hitColliderConfig = hitColliderConfig;
        characterConfig.grazeColliderConfig = grazeColliderConfig;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        var moveBoxCenter = transform.position + new Vector3(moveColliderConfig.offset.x, moveColliderConfig.offset.y);
        Gizmos.DrawWireCube(moveBoxCenter, new Vector3(moveColliderConfig.boxSize.x, moveColliderConfig.boxSize.y));

        Gizmos.color = Color.white;
        var hitSphereCenter = transform.position + new Vector3(hitColliderConfig.offset.x, hitColliderConfig.offset.y);
        Gizmos.DrawSphere(hitSphereCenter, hitColliderConfig.radius);

        Gizmos.color = Color.blue;
        var grazeSphereCenter = transform.position + new Vector3(grazeColliderConfig.offset.x, grazeColliderConfig.offset.y);
        Gizmos.DrawWireSphere(grazeSphereCenter, grazeColliderConfig.radius);
    }
}
