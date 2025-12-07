using UnityEngine;
using UnityEngine.UI;
public class WeaponSelectPanel : UIPanel
{
    [SerializeField] CharacterSelectPanel characterSelectPanel;
    [SerializeField] RectTransform headTextRect;

    [SerializeField] Image character;
    [SerializeField] Sprite[] characterSprites;
    Animator animator;
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        //InputManager.Instance.OnKeyInput_X += ExitWeaponSelect;
        UpdateWeaponCharacter();
        animator.Play("ShowWeaponPanel");
    }

    public void SelectWeapon(int id)
    {
        
    }

    void UpdateWeaponCharacter()
    {
        
    }
    void ExitWeaponSelect()
    {
       
        AudioManager.Instance.PlayAudio(AudioName.Cancel);
    }

    void OnDisable()
    {
        //InputManager.Instance.OnKeyInput_X -= ExitWeaponSelect;
    }
}
