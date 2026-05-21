using UnityEngine;

public abstract class SettingsSubPanelBase : UIPanel
{
    protected GameSettingsService Settings => GameSettingsService.Instance;

    protected void CommitChange(bool applyCategoryOnly = false)
    {
        if (applyCategoryOnly)
            ApplyCategory();
        else
            Settings.NotifyChanged(save: true, apply: true);
    }

    protected abstract void BindUI();
    protected abstract void ApplyCategory();
    protected abstract void RefreshUI();

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        Settings.EnsureLoaded();
        BindUI();
        RefreshUI();
    }
}
