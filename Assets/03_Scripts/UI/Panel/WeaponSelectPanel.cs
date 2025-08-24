using UnityEngine;
using UnityEngine.UI;
public class WeaponSelectPanel : BasePanel
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
        InputManager.Instance.OnKeyInput_X += ExitWeaponSelect;
        UpdateWeaponCharacter();
        animator.Play("ShowWeaponPanel");
    }

    public void SelectWeapon(int id)
    {
        E_CharacterName character = BattleManager.battleConfig.character;
        switch (character) 
        {
            case E_CharacterName.Reimu:
                switch (id)
                {
                    case 0:
                        BattleManager.battleConfig.weapon = E_Weapon.Weapon_Reimu_0;
                        break;
                    case 1:
                        BattleManager.battleConfig.weapon = E_Weapon.Weapon_Reimu_1;
                        break;
                    case 2:
                        BattleManager.battleConfig.weapon = E_Weapon.Weapon_Reimu_2;
                        break;
                }
            break;
            case E_CharacterName.Marisa:
                switch (id)
                {
                    case 0:
                        BattleManager.battleConfig.weapon = E_Weapon.Weapon_Marisa_0;
                        break;
                    case 1:
                        BattleManager.battleConfig.weapon = E_Weapon.Weapon_Marisa_1;
                        break;
                    case 2:
                        BattleManager.battleConfig.weapon = E_Weapon.Weapon_Marisa_2;
                        break;
                }
                break;
        }
    }

    void UpdateWeaponCharacter()
    {
        switch (BattleManager.battleConfig.character)
        {
            case E_CharacterName.Reimu:
                character.sprite = characterSprites[0];
                break;
            case E_CharacterName.Marisa:
                character.sprite = characterSprites[1];
                break;           
        }
    }
    void ExitWeaponSelect()
    {
        EnablePanel(false);
        characterSelectPanel.EnablePanel(true);
        AudioManager.Instance.PlayAudio(E_AudioName.Cancel);
    }

    void OnDisable()
    {
        InputManager.Instance.OnKeyInput_X -= ExitWeaponSelect;
    }
}
