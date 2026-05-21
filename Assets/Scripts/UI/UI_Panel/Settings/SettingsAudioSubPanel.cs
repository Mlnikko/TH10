using UnityEngine;
using UnityEngine.UI;

public class SettingsAudioSubPanel : SettingsSubPanelBase
{
    [Header("主音量")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Toggle masterMuteToggle;

    [Header("背景音乐音量")]
    [SerializeField] Slider bgmVolumeSlider;
    [SerializeField] Toggle bgmMuteToggle;

    [Header("UI音量")]
    [SerializeField] Slider uiVolumeSlider;
    [SerializeField] Toggle uiMuteToggle;

    [Header("音效音量")]
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Toggle sfxMuteToggle;

    bool _bound;

    protected override void BindUI()
    {
        if (_bound) return;
        _bound = true;

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (masterMuteToggle != null)
            masterMuteToggle.onValueChanged.AddListener(OnMasterMuteChanged);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        if (bgmMuteToggle != null)
            bgmMuteToggle.onValueChanged.AddListener(OnBgmMuteChanged);

        if (uiVolumeSlider != null)
            uiVolumeSlider.onValueChanged.AddListener(OnUiVolumeChanged);
        if (uiMuteToggle != null)
            uiMuteToggle.onValueChanged.AddListener(OnUiMuteChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (sfxMuteToggle != null)
            sfxMuteToggle.onValueChanged.AddListener(OnSfxMuteChanged);
    }

    protected override void RefreshUI()
    {
        var data = Settings.Data;

        RefreshChannel(masterVolumeSlider, masterMuteToggle, data.masterVolume, data.masterMuted);
        RefreshChannel(bgmVolumeSlider, bgmMuteToggle, data.bgmVolume, data.bgmMuted);
        RefreshChannel(uiVolumeSlider, uiMuteToggle, data.uiVolume, data.uiMuted);
        RefreshChannel(sfxVolumeSlider, sfxMuteToggle, data.sfxVolume, data.sfxMuted);
    }

    static void RefreshChannel(Slider slider, Toggle muteToggle, float volume, bool muted)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(volume);
            slider.interactable = !muted;
        }

        if (muteToggle != null)
            muteToggle.SetIsOnWithoutNotify(muted);
    }

    void OnMasterVolumeChanged(float value)
    {
        Settings.Data.masterVolume = value;
        CommitChange(applyCategoryOnly: true);
    }

    void OnMasterMuteChanged(bool muted) => OnChannelMuteChanged(
        muted, ref Settings.Data.masterMuted, masterVolumeSlider);

    void OnBgmVolumeChanged(float value)
    {
        Settings.Data.bgmVolume = value;
        CommitChange(applyCategoryOnly: true);
    }

    void OnBgmMuteChanged(bool muted) => OnChannelMuteChanged(
        muted, ref Settings.Data.bgmMuted, bgmVolumeSlider);

    void OnUiVolumeChanged(float value)
    {
        Settings.Data.uiVolume = value;
        CommitChange(applyCategoryOnly: true);
    }

    void OnUiMuteChanged(bool muted) => OnChannelMuteChanged(
        muted, ref Settings.Data.uiMuted, uiVolumeSlider);

    void OnSfxVolumeChanged(float value)
    {
        Settings.Data.sfxVolume = value;
        CommitChange(applyCategoryOnly: true);
    }

    void OnSfxMuteChanged(bool muted) => OnChannelMuteChanged(
        muted, ref Settings.Data.sfxMuted, sfxVolumeSlider);

    void OnChannelMuteChanged(bool muted, ref bool muteField, Slider slider)
    {
        muteField = muted;
        if (slider != null)
            slider.interactable = !muted;
        CommitChange(applyCategoryOnly: true);
    }

    protected override void ApplyCategory()
    {
        GameSettingsApplier.ApplyAudio(Settings.Data);
        Settings.Save();
    }
}
