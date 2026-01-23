using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIPanel : UIPanel
{
    [SerializeField] Image rankImage;
    [SerializeField] TMP_Text hiScoreValueText;
    [SerializeField] TMP_Text scoreValueText;
    [SerializeField] TMP_Text playerLifeValueText;
    [SerializeField] TMP_Text powerValueText;

    [SerializeField] Sprite[] rankSprite;

    public void SetRank(E_Rank rank)
    {
        switch (rank)
        {
            case E_Rank.Eazy:
                rankImage.sprite = rankSprite[0];
                break;
            case E_Rank.Normal:
                rankImage.sprite = rankSprite[1];
                break;
            case E_Rank.Hard:
                rankImage.sprite = rankSprite[2];
                break;
            case E_Rank.Lunatic:
                rankImage.sprite = rankSprite[3];
                break;
            case E_Rank.Extra:
                rankImage.sprite = rankSprite[4];
                break;
        }
    }

    void SetModeImage(Sprite sprite)
    {
        rankImage.sprite = sprite;
    }

    void SetHiScoreValue(int hiScore)
    {
        hiScoreValueText.text = hiScore.ToString();
    }

    void SetScoreValue(int score)
    {
        scoreValueText.text = score.ToString();
    }

    void SetPlayerLifeValue(int lifeCount)
    {   
        string lifeIcon = string.Empty;
        while (lifeCount > 0) 
        { 
            lifeCount--;
            lifeIcon += '♥';
        }
        playerLifeValueText.text = lifeIcon;
    }
}
