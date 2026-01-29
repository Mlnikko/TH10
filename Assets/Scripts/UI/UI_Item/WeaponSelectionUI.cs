using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class WeaponSelectionUI : MonoBehaviour
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
        nameLabel.text = config.description;

        string weaponAtlasKey = ResHelper.GetSpriteAtlasKey("Weapon");

        var atlas = await ResManager.LoadAsync<SpriteAtlas>(weaponAtlasKey);

        iconImage.sprite = atlas.GetSprite(config.ConfigId);
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
