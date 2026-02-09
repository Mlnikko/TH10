using UnityEngine;
using UnityEngine.UI;

public class WeaponButton : CustomButton
{
    [SerializeField] Image weaponName;
    [SerializeField] Sprite[] weaponNameSprites;

    [SerializeField] Image weaponDetails;
    [SerializeField] Sprite[] weaponDetailsSprites;

    [SerializeField] Image clearFlag;

    [SerializeField] float btnAnimDuration = 0.2f;

    [SerializeField] Color selectColor;
    [SerializeField] Color unSelectColor;

    CanvasGroup detailsCg;
    RectTransform detailsRect;

    [SerializeField] Vector2 move;
    Vector2 detailRectStartPos;
    Vector2 detailRectEndPos;



    protected override void OnButtonInit()
    {
        base.OnButtonInit();

        detailsCg = weaponDetails.GetComponent<CanvasGroup>();
        detailsRect = weaponDetails.GetComponent<RectTransform>();

        detailRectEndPos = detailsRect.anchoredPosition;
        detailRectStartPos = detailRectEndPos + move;

        weaponName.color = unSelectColor;
        detailsCg.alpha = 0;
    }

    void OnEnable()
    {
        UpdateWeaponSprites();
    }
    void UpdateWeaponSprites()
    {
        //switch (BattleManager.battleConfig.characterCfgIndex)
        //{
        //    case E_Character.Reimu:
        //        weaponName.sprite = weaponNameSprites[0];
        //        weaponDetails.sprite = weaponDetailsSprites[0];
        //        break;
        //    case E_Character.Marisa:
        //        weaponName.sprite = weaponNameSprites[1];
        //        weaponDetails.sprite = weaponDetailsSprites[1];
        //        break;
        //}
    }

    protected override void OnButtonSelected()
    {
        base.OnButtonSelected();
        weaponName.color = selectColor;
        detailsCg.Fade(1, btnAnimDuration);
        detailsRect.Move(detailRectStartPos, detailRectEndPos, btnAnimDuration);
    }

    protected override void OnButtonUnSelected()
    {
        base.OnButtonUnSelected();
        weaponName.color = unSelectColor;
        detailsCg.Fade(0, btnAnimDuration);
        detailsRect.Move(detailRectEndPos, detailRectStartPos , btnAnimDuration);
    }
}
