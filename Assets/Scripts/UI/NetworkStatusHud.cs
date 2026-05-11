using TMPro;
using UnityEngine;

/// <summary>
/// 联机角色 / RTT 状态 HUD：挂在 <see cref="UIManager"/> 的 UICanvas 右上角，替代 <see cref="NetworkManager"/> 的 IMGUI。
/// </summary>
public sealed class NetworkStatusHud : MonoBehaviour
{
    public const string HudChildName = "[NetworkStatusHud]";

    TextMeshProUGUI _label;

    void Awake()
    {
        _label = GetComponent<TextMeshProUGUI>();
        if (_label == null)
            _label = gameObject.AddComponent<TextMeshProUGUI>();

        if (_label.font == null && TMP_Settings.defaultFontAsset != null)
            _label.font = TMP_Settings.defaultFontAsset;

        _label.fontSize = 14f;
        _label.color = Color.white;
        _label.alignment = TextAlignmentOptions.TopRight;
        _label.enableWordWrapping = true;
        _label.richText = false;
        _label.raycastTarget = false;
        _label.overflowMode = TextOverflowModes.Overflow;
    }

    public static NetworkStatusHud GetOrCreate(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform existing = canvasTransform.Find(HudChildName);
        if (existing != null)
            return existing.GetComponent<NetworkStatusHud>();

        var go = new GameObject(HudChildName);
        go.transform.SetParent(canvasTransform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-12f, -12f);
        rt.sizeDelta = new Vector2(200f, 72f);

        go.AddComponent<TextMeshProUGUI>();
        return go.AddComponent<NetworkStatusHud>();
    }

    public void SetStatusText(string text)
    {
        if (_label != null)
            _label.text = text ?? string.Empty;
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
        if (visible)
            transform.SetAsLastSibling();
    }
}
