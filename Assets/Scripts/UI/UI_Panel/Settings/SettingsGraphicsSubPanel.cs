using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsGraphicsSubPanel : SettingsSubPanelBase
{
    static readonly E_DisplayRefreshRate[] RefreshRateOptions =
    {
        E_DisplayRefreshRate.Hz60,
        E_DisplayRefreshRate.Hz120,
    };

    [Header("刷新率")]
    [SerializeField] TMP_Dropdown refreshRateDropdown;

    [Header("窗口")]
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] TMP_Dropdown resolutionDropdown;

    bool _bound;
    bool _refreshRateOptionsBuilt;

    protected override void BindUI()
    {
        if (_bound) return;
        _bound = true;

        EnsureRefreshRateDropdownOptions();

        if (refreshRateDropdown != null)
            refreshRateDropdown.onValueChanged.AddListener(OnRefreshRateChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    void EnsureRefreshRateDropdownOptions()
    {
        if (_refreshRateOptionsBuilt || refreshRateDropdown == null) return;

        refreshRateDropdown.ClearOptions();
        refreshRateDropdown.AddOptions(new List<string> { "60 Hz", "120 Hz" });
        _refreshRateOptionsBuilt = true;
    }

    protected override void RefreshUI()
    {
        var data = Settings.Data;

        EnsureRefreshRateDropdownOptions();
        if (refreshRateDropdown != null)
        {
            int rateIndex = RefreshRateToDropdownIndex(data.displayRefreshRate);
            refreshRateDropdown.SetValueWithoutNotify(rateIndex);
            refreshRateDropdown.RefreshShownValue();
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var preset in DisplayResolutionCatalog.All)
                options.Add(preset.Label);
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(data.resolutionPresetIndex, 0, options.Count - 1));
            resolutionDropdown.RefreshShownValue();
        }

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(data.windowMode == E_WindowMode.FullScreen);
    }

    void OnRefreshRateChanged(int index)
    {
        index = Mathf.Clamp(index, 0, RefreshRateOptions.Length - 1);
        Settings.Data.displayRefreshRate = RefreshRateOptions[index];
        CommitChange(applyCategoryOnly: true);
    }

    static int RefreshRateToDropdownIndex(E_DisplayRefreshRate rate)
    {
        for (int i = 0; i < RefreshRateOptions.Length; i++)
        {
            if (RefreshRateOptions[i] == rate)
                return i;
        }
        return 0;
    }

    void OnFullscreenChanged(bool isFullscreen)
    {
        Settings.Data.windowMode = isFullscreen ? E_WindowMode.FullScreen : E_WindowMode.Windowed;
        CommitChange(applyCategoryOnly: true);
    }

    void OnResolutionChanged(int index)
    {
        DisplayResolutionCatalog.ApplyPresetToData(Settings.Data, index);
        CommitChange(applyCategoryOnly: true);
    }

    protected override void ApplyCategory()
    {
        GameSettingsApplier.ApplyGraphics(Settings.Data);
        Settings.Save();
    }
}
