using UnityEngine;

public class CharacterSelectPanel : BasePanel
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
        InputManager.Instance.OnKeyInput_X += ExitCharacterSelect;
        animator.Play("ShowCharacterPanel");
    }

    void ExitCharacterSelect()
    {
        EnablePanel(false);
        rankSelectPanel.EnablePanel(true);
        AudioManager.Instance.PlayAudio(AudioName.Cancel);     
    }
    public void SelectCharacterWeapon(int character)
    {
        BattleManager.Instance.battleSession.character = (E_Character)character;
        EnterWeaponSelect();
    }

    void EnterWeaponSelect()
    {
        weaponSelectPanel.EnablePanel(true);
        EnablePanel(false);
    }

    void OnDisable()
    {
        InputManager.Instance.OnKeyInput_X -= ExitCharacterSelect;
    }
}
