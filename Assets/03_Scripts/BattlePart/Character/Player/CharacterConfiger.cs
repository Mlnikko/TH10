using UnityEngine;
public class CharacterConfiger : MonoBehaviour
{
    public CharacterConfig CharacterConfig
    {
        get
        {
            return characterConfig;
        }
    }

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

    //[Header("受击碰撞体设置")]
    //[SerializeField] ColliderComponent bodyCollider;
    //[SerializeField] float hitRadius;

    //[Header("擦弹碰撞体设置")]
    //[SerializeField] ColliderComponent grazeCollider;
    //[SerializeField] float grazeRadius; 

    void Awake()
    {
        LoadCharacterConfig();
    }

    public void LoadCharacterConfig()
    {
        characterName = characterConfig.CharacterID;
        description = characterConfig.Description;
        speed = characterConfig.MoveSpeed;
        slowSpeed = characterConfig.MoveSlowSpeed;
        moveBoxSize = characterConfig.MoveBoxSize;
        moveBoxOffset = characterConfig.MoveBoxOffset;
        //hitRadius = characterConfig.HitRadius;
        //grazeRadius = characterConfig.GrazeRadius;
        //if(bodyCollider != null)
        //    bodyCollider.Radius = hitRadius;
        //if(grazeCollider != null)
        //    grazeCollider.Radius = grazeRadius;
    }

    public void SaveCharacterConfig()
    {
        if (characterConfig == null) return;

        characterConfig.CharacterID = characterName;
        characterConfig.Description = description;
        characterConfig.MoveSpeed = speed;
        characterConfig.MoveSlowSpeed = slowSpeed;
        characterConfig.MoveBoxSize = moveBoxSize;
        characterConfig.MoveBoxOffset = moveBoxOffset;
        //characterConfig.HitRadius = hitRadius;
        //characterConfig.GrazeRadius = grazeRadius;

        UnityEditor.EditorUtility.SetDirty(characterConfig);
        UnityEditor.AssetDatabase.SaveAssets();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)moveBoxOffset, moveBoxSize);

        //Gizmos.color = Color.white;
        //Gizmos.DrawSphere(transform.position, hitRadius);

        //Gizmos.color = Color.blue;
        //Gizmos.DrawWireSphere(transform.position, grazeRadius);
    }
}
