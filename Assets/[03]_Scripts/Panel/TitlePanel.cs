using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TitlePanel : BasePanel
{
    [SerializeField] CanvasGroup cg_Tips;

    void Start()
    {
        LoopBlink(cg_Tips, 1f, 0.5f, 0.8f);
    }
}
