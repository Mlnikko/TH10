using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    public TMP_Text nameLabel;
    public Image iconImage;
    public Button selectButton;

    public string ConfigId { get; private set; }

    Action onSelect;

    void OnEnable()
    {
        selectButton.onClick.AddListener(OnClick);
    }

    public void Initialize(CharacterConfig config, Action onSelect)
    {
        this.onSelect = onSelect;
        ConfigId = config.ConfigId;
        nameLabel.text = config.description;

        var sprite = GameResDB.GetSpriteFromTexture(config.characterID.ToString().ToLowerInvariant());

        iconImage.sprite = sprite;
    }

    public void SetSelected(bool selected)
    {
        // 假设有一个 "Selected" 状态颜色
        var color = selected ? Color.yellow : Color.white;
        nameLabel.color = color;
        // 或者启用/禁用某个高亮 Texture
    }

    public void OnClick()
    {
        onSelect?.Invoke();
    }

    void OnDisable()
    {
        selectButton.onClick.RemoveListener(OnClick);
    }
}
