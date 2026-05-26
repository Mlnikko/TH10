using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoItem : MonoBehaviour
{
    public Image hostIndicator;
    public Image playerImage;
    public TMP_Text playerNameText;

    public void SetOccupied(int slotIndex, bool isHost = false)
    {
        playerNameText.text = isHost ? $"玩家 {slotIndex + 1}（房主）" : $"玩家 {slotIndex + 1}";
        hostIndicator.gameObject.SetActive(isHost);
    }

    public void SetEmpty(int slotIndex)
    {
        playerNameText.text = $"玩家 {slotIndex + 1}（空）";
        hostIndicator.gameObject.SetActive(false);
    }
}
