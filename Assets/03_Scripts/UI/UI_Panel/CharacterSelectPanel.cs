using UnityEngine;

public class CharacterSelectPanel : UIPanel
{
    [SerializeField] RankSelectPanel rankSelectPanel;
    [SerializeField] WeaponSelectPanel weaponSelectPanel;
    Animator animator;
    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    void OnEnable()
    {
        //InputManager.Instance.OnKeyInput_X += ExitCharacterSelect;
        animator.Play("ShowCharacterPanel");
    }

    void ExitCharacterSelect()
    {
       
        AudioManager.Instance.PlayAudio(AudioName.Cancel);     
    }
    public void SelectCharacterWeapon(int character)
    {
        //BattleManager.Instance.battleSession.characterId = (E_Character)characterId;
        EnterWeaponSelect();
    }

    void EnterWeaponSelect()
    {
       
    }

    void OnDisable()
    {
        //InputManager.Instance.OnKeyInput_X -= ExitCharacterSelect;
    }
}
