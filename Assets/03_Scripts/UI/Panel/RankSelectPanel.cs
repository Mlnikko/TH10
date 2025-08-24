using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RankSelectPanel : BasePanel
{
    [SerializeField] TitlePanel titlePanel;
    [SerializeField] CharacterSelectPanel characterSelectPanel;
    [SerializeField] ButtonGroupController rankBtnGroup;
    Animator animator;
    void Awake()
    {
        animator = GetComponent<Animator>();      
    }
    void OnEnable()
    {
        InputManager.Instance.OnKeyInput_X += ExitRankSelect;
        animator.Play("ShowRankSelectPanel");
    }
    void ExitRankSelect()
    {
        PanelFadeOut();
        AudioManager.Instance.PlayAudio(E_AudioName.Cancel);
        titlePanel.EnableBtnGroupNavigation(true);
    }

    public void SelectRank(int rank)
    {
        BattleManager.battleConfig.rank = (E_Rank)rank;
        EnterCharacterSelect();
    }

    void EnterCharacterSelect()
    {
        EnablePanel(false);
        characterSelectPanel.EnablePanel(true);  
    }

    void OnDisable()
    {
        InputManager.Instance.OnKeyInput_X -= ExitRankSelect;
    }
}
