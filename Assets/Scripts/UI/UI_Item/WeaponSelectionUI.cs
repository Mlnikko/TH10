using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectionUI : MonoBehaviour
{
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
        nameLabel.text = config.description;
        weaponId = config.weaponID;

        var sprite = GameResDB.Instance.GetSpriteFromAtlas("weapon", weaponId.ToString().ToLowerInvariant());

        if(sprite == null)
        {
            Logger.Warn($"Weapon icon sprite not found for configId: {weaponId}");
        }

        iconImage.sprite = sprite;
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
