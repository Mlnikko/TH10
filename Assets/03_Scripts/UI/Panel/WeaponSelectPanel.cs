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
        //InputManager.Instance.OnKeyInput_X += ExitWeaponSelect;
        UpdateWeaponCharacter();
        animator.Play("ShowWeaponPanel");
    }

    public void SelectWeapon(int id)
    {
        //E_Character characterId = BattleManager.Instance.battleSession.characterId;
        //switch (characterId) 
        //{
        //    case E_Character.Reimu:
        //        switch (id)
        //        {
        //            case 0:
        //                BattleManager.Instance.battleSession.WeaponID = E_Weapon.Weapon_Reimu_0;
        //                break;
        //            case 1:
        //                BattleManager.Instance.battleSession.WeaponID = E_Weapon.Weapon_Reimu_1;
        //                break;
        //            case 2:
        //                BattleManager.Instance.battleSession.WeaponID = E_Weapon.Weapon_Reimu_2;
        //                break;
        //        }
        //    break;
        //    case E_Character.Marisa:
        //        switch (id)
        //        {
        //            case 0:
        //                BattleManager.Instance.battleSession.WeaponID = E_Weapon.Weapon_Marisa_0;
        //                break;
        //            case 1:
        //                BattleManager.Instance.battleSession.WeaponID = E_Weapon.Weapon_Marisa_1;
        //                break;
        //            case 2:
        //                BattleManager.Instance.battleSession.WeaponID = E_Weapon.Weapon_Marisa_2;
        //                break;
        //        }
        //        break;
        //}
    }

    void UpdateWeaponCharacter()
    {
        //switch (BattleManager.Instance.battleSession.characterId)
        //{
        //    case E_Character.Reimu:
        //        characterId.sprite = characterSprites[0];
        //        break;
        //    case E_Character.Marisa:
        //        characterId.sprite = characterSprites[1];
        //        break;           
        //}
    }
    void ExitWeaponSelect()
    {
        EnablePanel(false);
        characterSelectPanel.EnablePanel(true);
        AudioManager.Instance.PlayAudio(AudioName.Cancel);
    }

    void OnDisable()
    {
        //InputManager.Instance.OnKeyInput_X -= ExitWeaponSelect;
    }
}
