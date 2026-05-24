using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    [SerializeField] TMP_Text playerId;
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] Image iconImage;
    [SerializeField] Button selectButton;
    public E_Character characterName;

    Action onSelect;

    void OnEnable()
    {
        selectButton.onClick.AddListener(OnClick);
    }

    public void Initialize(CharacterConfig config, Action onSelect)
    {
        this.onSelect = onSelect;     
        nameLabel.text = config.description;
        characterName = config.character;

        string characterId = characterName.ToString().ToLowerInvariant();
        Sprite sprite = GameResDB.Instance.GetSpriteFromTexture(characterId);

        if(sprite == null)
        {
            Logger.Warn($"character icon sprite not found for configId: {characterName}");
        }

        iconImage.sprite = sprite;
        SetOccupyingPlayerId(null);
    }

    /// <summary>显示已确认准备并锁定该角色的玩家（如 P1）；无则隐藏。</summary>
    public void SetOccupyingPlayerId(byte? playerIndex)
    {
        if (playerId == null)
            return;

        if (!playerIndex.HasValue)
        {
            playerId.gameObject.SetActive(false);
            return;
        }

        playerId.gameObject.SetActive(true);
        playerId.text = $"P{playerIndex.Value + 1}";
    }

    public void SetSelected(bool selected)
    {
        var color = selected ? Color.yellow : Color.white;
        nameLabel.color = color;
    }

    public void SetInteractable(bool interactable)
    {
        if (selectButton != null)
            selectButton.interactable = interactable;
    }

    public void SetTakenByOther(bool takenByOther)
    {
        if (nameLabel == null) return;
        nameLabel.color = takenByOther ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
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
