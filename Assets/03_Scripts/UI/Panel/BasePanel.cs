using UnityEngine;
using DG.Tweening;

public class BasePanel : MonoBehaviour
{
    [SerializeField] E_Panel panelName;
    CanvasGroup panelCanvasGroup;
    public E_Panel PanelName => panelName;
    public void EnablePanel(bool enable)
    {
        if (enable)
        {
            gameObject.SetActive(true);       
        }
        else
        {
            gameObject.SetActive(false);          
        }
    }
    public void PanelFadeIn(float duration = 0.2f, float end = 1)
    {
        EnablePanel(true);
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        panelCanvasGroup.Fade(end, duration);
    }
    public void PanelFadeOut(float duration = 0.2f, float end = 0, bool isEndDisabled = true)
    {
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (isEndDisabled)
        {
            panelCanvasGroup.Fade(end, duration).OnComplete(() => EnablePanel(false));
        }
        else
        {
            panelCanvasGroup.Fade(end, duration);
        }
    }
    public void DestroyPanel(bool instant)
    {
        EnablePanel(false);

        if (instant) DestroyImmediate(gameObject);
        else Destroy(gameObject);
    }
}
