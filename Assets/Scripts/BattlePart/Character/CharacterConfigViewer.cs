using UnityEngine;

public class CharacterConfigViewer : MonoBehaviour
{
    public CharacterConfig CharacterConfig => characterConfig;

    [SerializeField] CharacterConfig characterConfig;

    [Header("信息配置")]
    [SerializeField] E_Character characterName;

    [TextArea(1, 5)]
    [SerializeField] string description;

    [Header("生命配置")]
    [SerializeField] int maxHealth;

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

        maxHealth = characterConfig.maxHealth;

        speed = characterConfig.moveSpeed;
        slowSpeed = characterConfig.moveSlowSpeed;

        moveColliderConfig = characterConfig.moveColliderConfig;
        hitColliderConfig = characterConfig.hitColliderConfig;
        grazeColliderConfig = characterConfig.grazeColliderConfig;
    }

    public void SaveCharacterConfig()
    {
        if (characterConfig == null) return;

        characterConfig.character = characterName;
        characterConfig.description = description;

        characterConfig.maxHealth = maxHealth;

        characterConfig.moveSpeed = speed;
        characterConfig.moveSlowSpeed = slowSpeed;

        characterConfig.moveColliderConfig = moveColliderConfig;
        characterConfig.hitColliderConfig = hitColliderConfig;
        characterConfig.grazeColliderConfig = grazeColliderConfig;
    }

    void OnDrawGizmosSelected()
    {
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, moveColliderConfig, Color.cyan, Color.cyan);
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, hitColliderConfig, Color.red, Color.red);
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, grazeColliderConfig, Color.blue, Color.blue);
    }
}
