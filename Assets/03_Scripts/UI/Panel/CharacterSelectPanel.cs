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
        AudioManager.Instance.PlayAudio(E_AudioName.Cancel);     
    }
    public void SelectCharacterWeapon(int character)
    {
        BattleManager.battleConfig.character = (E_CharacterName)character;
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
