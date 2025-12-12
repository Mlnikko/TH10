using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponItemUI : MonoBehaviour
{
    public TMP_Text nameLabel;
    public Image iconImage;
    public Button selectButton;

    public string ConfigId { get; private set; }

    System.Action onSelect;

    void OnEnable()
    {
        selectButton.onClick.AddListener(OnClick);
    }

    public async Task Initialize(WeaponConfig config, System.Action onSelect)
    {
        this.onSelect = onSelect;
        ConfigId = config.ConfigId;
        nameLabel.text = config.Description;

        string weaponSpriteKey = SpriteHelper.GetWeaponSpriteKey(config.WeaponID.ToString());

        await SpriteManager.LoadSpriteAsync(weaponSpriteKey);
        iconImage.sprite = SpriteManager.GetSprite(weaponSpriteKey);
    }

    public void SetSelected(bool selected)
    {
        nameLabel.color = selected ? Color.green : Color.white;
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
