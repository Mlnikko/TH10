using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RankSelectPanel : UIPanel
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
        //InputManager.Instance.OnKeyInput_X += ExitRankSelect;
        animator.Play("ShowRankSelectPanel");
    }
    void ExitRankSelect()
    {

        AudioManager.Instance.PlayAudio(AudioName.Cancel);
        titlePanel.EnableBtnGroupNavigation(true);
    }

    public void SelectRank(int rank)
    {
        //BattleManager.Instance.battleSession.rank = (E_Rank)rank;
        EnterCharacterSelect();
    }

    void EnterCharacterSelect()
    {
        
    }

    void OnDisable()
    {
        //InputManager.Instance.OnKeyInput_X -= ExitRankSelect;
    }
}
