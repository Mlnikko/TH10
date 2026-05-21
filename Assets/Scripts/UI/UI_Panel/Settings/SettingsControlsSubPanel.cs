using UnityEngine;
using UnityEngine.UI;

public class SettingsControlsSubPanel : SettingsSubPanelBase
{
    [SerializeField] SettingsKeyBindButton[] keyBindButtons;
    [SerializeField] Button resetDefaultsButton;

    bool _bound;

    protected override void BindUI()
    {
        if (_bound) return;
        _bound = true;

        if (keyBindButtons != null)
        {
            foreach (var row in keyBindButtons)
            {
                if (row != null)
                    row.Setup(OnKeyBindingChanged);
            }
        }

        if (resetDefaultsButton != null)
            resetDefaultsButton.onClick.AddListener(ResetKeyBindingsToDefault);
    }

    void OnKeyBindingChanged()
    {
        CommitChange(applyCategoryOnly: true);
    }

    void ResetKeyBindingsToDefault()
    {
        var defaults = Resources.Load<GameSettingsConfig>("GameSettingsConfig");
        if (defaults != null)
            Settings.Data.keyBindings = JsonUtility.FromJson<InputKeyCodeConfig>(
                JsonUtility.ToJson(defaults.data.keyBindings));
        else
            Settings.Data.keyBindings = new InputKeyCodeConfig();

        RefreshUI();
        CommitChange(applyCategoryOnly: true);
    }

    protected override void RefreshUI()
    {
        if (keyBindButtons == null) return;
        foreach (var row in keyBindButtons)
        {
            if (row != null)
                row.RefreshDisplay();
        }
    }

    protected override void ApplyCategory()
    {
        GameSettingsApplier.ApplyControls(Settings.Data);
        Settings.Save();
    }
}
