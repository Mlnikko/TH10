using UnityEngine;

public class CharacterConfigViewer : MonoBehaviour
{
    public CharacterConfig CharacterConfig => characterConfig;

    [SerializeField] CharacterConfig characterConfig;

    [Header("–≈œ¢≈‰÷√")]
    [SerializeField] E_Character characterName;

    [TextArea(1, 5)]
    [SerializeField] string description;

    [Header("“∆ÀŸ≈‰÷√")]
    [SerializeField] float speed;
    [SerializeField] float slowSpeed;

    [Header("“∆∂Ø≈ˆ◊≤ÃÂ≈‰÷√")]
    [SerializeField] ColliderConfig moveColliderConfig;

    [Header(" ‹ª˜≈ˆ◊≤ÃÂ≈‰÷√")]
    [SerializeField] ColliderConfig hitColliderConfig;

    [Header("≤¡µØ≈ˆ◊≤ÃÂ≈‰÷√")]
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
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, moveColliderConfig, Color.cyan, Color.cyan);
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, hitColliderConfig, Color.red, Color.red);
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, grazeColliderConfig, Color.blue, Color.blue);
    }
}
