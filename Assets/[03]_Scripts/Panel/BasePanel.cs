using DG.Tweening;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    protected void LoopBlink(CanvasGroup cg, float startFade, float targetFade, float duration)
    {
        cg.alpha = startFade;
        cg.DOFade(targetFade, duration).SetLoops(-1, LoopType.Yoyo);
    }
}
