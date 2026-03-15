using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_UnitTest_AddEnemy : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_InputField inputField;

    void Start()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        if (int.TryParse(inputField.text, out int enemyCount))
        {
            
        }
        else
        {
            Logger.Warn("Invalid input for enemy count. Please enter a valid integer.", LogTag.UnitTest);
        }
    }
}
