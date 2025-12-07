using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : UIPanel
{
    [SerializeField] Image bgImage;
    [SerializeField] CanvasGroup cg_image1;
    [SerializeField] CanvasGroup cg_image2;

    void OnEnable()
    {
        cg_image1.Fade(1, 0.5f, 0.8f, -1);
        cg_image2.Fade(0.5f, 1f, 0.8f, -1);
    }
}
