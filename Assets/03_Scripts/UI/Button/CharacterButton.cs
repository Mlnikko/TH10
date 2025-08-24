using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : CustomButton
{
    [SerializeField] Image character;
    [SerializeField] Image description;

    [SerializeField] float btnAnimDuration;
    [SerializeField] Vector2 startPosOffset;
    [SerializeField] Color selectedColor;
    [SerializeField] Color unSelectedColor;

    RectTransform descriptionRect;
    Vector2 startPos;
    Vector2 endPos;

    protected override void OnButtonInit()
    {
        base.OnButtonInit();
        descriptionRect = description.GetComponent<RectTransform>();
        startPos = descriptionRect.anchoredPosition + startPosOffset;
        endPos = descriptionRect.anchoredPosition;
        character.color = unSelectedColor;
        description.color = unSelectedColor;
    }

    protected override void OnButtonSelected()
    {
        base.OnButtonSelected();
        character.Color(selectedColor, btnAnimDuration);
        description.Color(selectedColor, btnAnimDuration);
        descriptionRect.Move(startPos, endPos, btnAnimDuration);
    }

    protected override void OnButtonUnSelected()
    {
        base.OnButtonUnSelected();
        character.Color(unSelectedColor, btnAnimDuration);
        description.Color(unSelectedColor, btnAnimDuration);
        descriptionRect.Move(endPos, startPos, btnAnimDuration);
    }
}
