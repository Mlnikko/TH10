using System.Text;
using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] TMP_Text fpsText;
    StringBuilder _sb = new(10); // ºı…ŸGC

    void OnEnable()
    {
        SubscribeToFPS();
    }

    void OnDisable()
    {
        UnsubscribeFromFPS();
    }

    void SubscribeToFPS()
    {
        if (GameManager.Instance == null) return;

        // ±‹√‚÷ÿ∏¥∂©‘ƒ
        GameManager.Instance.UpdateFPS -= UpdateFPS;
        GameManager.Instance.UpdateFPS += UpdateFPS;
    }

    void UnsubscribeFromFPS()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UpdateFPS -= UpdateFPS;
    }

    void UpdateFPS(float value)
    {
        if (fpsText == null) return;
        _sb.Clear();
        _sb.AppendFormat("{0:F1} fps", value);
        fpsText.SetText(_sb);
    }
}
