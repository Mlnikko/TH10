using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionUI : MonoBehaviour
{
    const bool ShowWeaponIcon = false;

    public TMP_Text nameLabel;
    public Image iconImage;
    public Button selectButton;
    public E_Weapon weaponId;
    System.Action onSelect;

    void OnEnable()
    {
        selectButton.onClick.AddListener(OnClick);
    }

    public void Initialize(WeaponConfig config, System.Action onSelect)
    {
        this.onSelect = onSelect;
        nameLabel.text = config.GetSelectionDisplayName();
        weaponId = config.weaponID;

        if (iconImage == null)
            return;

        if (!ShowWeaponIcon)
        {
            iconImage.gameObject.SetActive(false);
            return;
        }

        iconImage.gameObject.SetActive(true);
        string spriteId = config.ConfigId;
        var sprite = GameResDB.Instance.GetSpriteFromAtlas("weapon", spriteId);

        if (sprite == null)
        {
            Logger.Warn(
                $"Weapon icon sprite not found: '{spriteId}' (weapon config: {config.ConfigId})",
                LogTag.Resource);
        }

        iconImage.sprite = sprite;
    }

    public void SetSelected(bool selected)
    {
        nameLabel.color = selected ? Color.green : Color.white;
    }

    public void SetInteractable(bool interactable)
    {
        if (selectButton != null)
            selectButton.interactable = interactable;
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
