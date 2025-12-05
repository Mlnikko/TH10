using System.Collections;
using UnityEngine;

public class TitlePanel : BasePanel
{
    [SerializeField] BasePanel rankSelectPanel;

    [SerializeField] CanvasGroup cg_Tips;
    [SerializeField] RectTransform logoRectTransform;
    [SerializeField] Animator animator;
    [SerializeField] TitleBtnGroup titleBtnGroup;
    bool isTipsShown;
    void Awake()
    {
        isTipsShown = true;       
        //InputManager.Instance.OnKeyInput_Any += GameFirstEnterHandler;
    }
    void Start()
    {
        cg_Tips.gameObject.SetActive(true);
        cg_Tips.Fade(1, 0.5f, 0.8f, -1);
    }

    #region 处理首次进入游戏的提示逻辑
    void GameFirstEnterHandler()
    {
        if (!isTipsShown) return;
        isTipsShown = false;
        AudioManager.Instance.PlayAudio(AudioName.Pause);
        //InputManager.Instance.OnKeyInput_Any -= GameFirstEnterHandler;
        StartCoroutine(ShowSelections());
    }
    IEnumerator ShowSelections()
    {
        cg_Tips.Fade(1, 0.5f, 0.1f, 8);
        yield return new WaitForSeconds(0.8f);
        cg_Tips.Fade(0, 0.2f);

        animator.Play("ShowSelections");
    }
    #endregion

    public void EnterRankSelectPanel()
    {
        EnableBtnGroupNavigation(false);
        rankSelectPanel.PanelFadeIn();
    }

    public void ReturnTitle()
    {
        EnableBtnGroupNavigation(true);
    }

    public void EnableBtnGroupNavigation(bool enable)
    {
        titleBtnGroup.EnableNavigation(enable);
    }
}
