using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankButton : CustomButton
{
    [SerializeField] Image btnImage;
    [SerializeField] Sprite selectedSprite;
    [SerializeField] Sprite unSelectedSprite;

    protected override void OnButtonInit()
    {
        base.OnButtonInit();
        btnImage.sprite = unSelectedSprite;
    }
    protected override void OnButtonSelected()
    {
        base.OnButtonSelected();
        btnImage.sprite = selectedSprite;
    }

    protected override void OnButtonUnSelected()
    {
        base.OnButtonUnSelected();
        btnImage.sprite = unSelectedSprite;
    }
}
