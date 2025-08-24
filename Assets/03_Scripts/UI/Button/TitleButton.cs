using TMPro;
using UnityEngine;

public class TitleButton : CustomButton
{
    [SerializeField] TMP_Text text;
    [SerializeField] Color startColor;
    [SerializeField] Color endColor;
    [SerializeField] float colorAnimDuration;

    protected override void OnButtonSelected()
    {
        base.OnButtonSelected();
        if (text != null) 
        {
            text.Color(startColor, endColor, colorAnimDuration, -1);
        }
    }

    protected override void OnButtonUnSelected()
    {
        base.OnButtonUnSelected();
        if (text != null) 
        {
            text.Color(startColor, 0.1f);
        }
    }
}
