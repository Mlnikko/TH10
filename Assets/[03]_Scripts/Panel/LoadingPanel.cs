using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : BasePanel
{
    [SerializeField] Image bgImage;
    [SerializeField] CanvasGroup cg_image1;
    [SerializeField] CanvasGroup cg_image2;

    void Start()
    {
        LoopBlink(cg_image1, 1, 0.5f, 0.8f);
        LoopBlink(cg_image2, 0.3f, 1f, 0.8f);
    }

    void ShowLoading()
    {

    }

    void HideLoading()
    {

    }
}
