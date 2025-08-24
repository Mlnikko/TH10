using UnityEngine;
public class CharacterPrefab : MonoBehaviour
{
    public CharacterConfig CharacterConfig;

    [Header("–≈œ¢≈‰÷√")]
    [SerializeField] E_CharacterName characterName;

    [TextArea(1, 5)]
    [SerializeField] string description;

    [Header("“∆ÀŸ≈‰÷√")]
    [SerializeField] float speed;
    [SerializeField] float slowSpeed;

    [Header("“∆∂Ø≈ˆ◊≤ÃÂ…Ë÷√")]
    [SerializeField] Vector2 moveBoxSize;
    [SerializeField] Vector2 moveBoxOffset;

    [Header(" ‹ª˜≈ˆ◊≤ÃÂ…Ë÷√")]
    [SerializeField] float hitRadius;

    [Header("≤¡µØ∞Îæ∂")]
    [SerializeField] float grazeRadius; 

    void Awake()
    {
        LoadCharacterConfig();
    }

    public void LoadCharacterConfig()
    {
        characterName = CharacterConfig.CharacterName;
        description = CharacterConfig.Description;
        speed = CharacterConfig.Speed;
        slowSpeed = CharacterConfig.SlowSpeed;
        moveBoxSize = CharacterConfig.MoveBoxSize;
        moveBoxOffset = CharacterConfig.MoveBoxOffset;
        hitRadius = CharacterConfig.HitRadius;
        grazeRadius = CharacterConfig.GrazeRadius;
    }

    public void SaveCharacterConfig()
    {
        if (CharacterConfig == null) return;

        CharacterConfig.CharacterName = characterName;
        CharacterConfig.Description = description;
        CharacterConfig.Speed = speed;
        CharacterConfig.SlowSpeed = slowSpeed;
        CharacterConfig.MoveBoxSize = moveBoxSize;
        CharacterConfig.MoveBoxOffset = moveBoxOffset;
        CharacterConfig.HitRadius = hitRadius;
        CharacterConfig.GrazeRadius = grazeRadius;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)moveBoxOffset, moveBoxSize);
    }
}
