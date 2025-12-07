using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoItem : MonoBehaviour
{
    public Image hostIndicator;
    public Image playerImage;
    public TMP_Text playerNameText;

    public void SetInfo(string name, bool isHost = false)
    {
        playerNameText.text = isHost ? $"{name} (·¿Ö÷)" : name;
        hostIndicator.gameObject.SetActive(isHost);
    }

    public void SetEmpty(int slotIndex)
    {
        playerNameText.text = $"Player {slotIndex + 1}£¨¿Õ£©";
        hostIndicator.gameObject.SetActive(false);
    }
}
