using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 帧同步输入调试 HUD：挂在 <see cref="UIManager"/> 的 UICanvas 下，走 CanvasRenderer，
/// 避免 <see cref="MonoBehaviour.OnGUI"/> IMGUI 与场景/UI 叠加剧烈重绘。
/// </summary>
public sealed class InputDebugHud : MonoBehaviour
{
    public const string HudChildName = "[InputDebugHud]";

    TextMeshProUGUI _label;

    void Awake()
    {
        _label = GetComponent<TextMeshProUGUI>();
        if (_label == null)
            _label = gameObject.AddComponent<TextMeshProUGUI>();

        if (_label.font == null && TMP_Settings.defaultFontAsset != null)
            _label.font = TMP_Settings.defaultFontAsset;

        _label.fontSize = 15f;
        _label.color = Color.white;
        _label.alignment = TextAlignmentOptions.TopLeft;
        _label.enableWordWrapping = true;
        _label.richText = true;
        _label.raycastTarget = false;
        _label.overflowMode = TextOverflowModes.Overflow;
    }

    /// <summary>在指定 Canvas 根下创建或获取 HUD（与战斗 UI 同画布，排序靠 <see cref="SetVisible"/>）。</summary>
    public static InputDebugHud GetOrCreate(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform existing = canvasTransform.Find(HudChildName);
        if (existing != null)
            return existing.GetComponent<InputDebugHud>();

        var go = new GameObject(HudChildName);
        go.transform.SetParent(canvasTransform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(12f, -12f);
        rt.sizeDelta = new Vector2(460f, 260f);

        go.AddComponent<TextMeshProUGUI>();
        return go.AddComponent<InputDebugHud>();
    }

    public void SetDebugText(StringBuilder sb)
    {
        if (_label == null || sb == null)
            return;
        // TMP 3.0.x 对 StringBuilder 重载因版本而异；调试 HUD 可接受每帧一次 ToString。
        _label.text = sb.ToString();
    }

    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
        if (visible)
            transform.SetAsLastSibling();
    }
}
