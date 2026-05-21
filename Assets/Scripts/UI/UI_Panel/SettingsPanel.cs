using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SettingCategory
{
    Graphics,
    Audio,
    Controls,
}

[Serializable]
public class SettingCategoryEntry
{
    public SettingCategory category;
    public Button categoryButton;
    public SettingsSubPanelBase subPanel;
}

/// <summary>
/// 分类按钮与子面板均在 SettingsPanel 预制体中预先摆放（子面板默认禁用），运行时只切换显示。
/// </summary>
public class SettingsPanel : UIPanel
{
    [SerializeField] Button returnButton;
    [SerializeField] SettingCategoryEntry[] categories;

    SettingCategory _currentCategory = SettingCategory.Graphics;
    bool _initialized;

    public override void Initialize()
    {
        base.Initialize();
        if (_initialized) return;
        _initialized = true;

        if (returnButton != null)
            returnButton.onClick.AddListener(() => UIManager.Instance.GoBack());

        if (categories == null) return;

        foreach (var entry in categories)
        {
            if (entry == null) continue;

            if (entry.subPanel != null)
            {
                entry.subPanel.Initialize();
                entry.subPanel.gameObject.SetActive(false);
            }

            if (entry.categoryButton != null)
            {
                var localCat = entry.category;
                entry.categoryButton.onClick.AddListener(() => ShowCategory(localCat));

                var text = entry.categoryButton.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = GetCategoryDisplayName(entry.category);
            }
        }
    }

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        if (!_initialized)
            Initialize();

        GameSettingsService.Instance.EnsureLoaded();
        ShowCategory(_currentCategory);
    }

    public override void OnHide()
    {
        base.OnHide();
        GameSettingsService.Instance.Save();

        var current = GetSubPanel(_currentCategory);
        if (current != null)
            current.OnHide();

        HideAllSubPanels();
    }

    void ShowCategory(SettingCategory category)
    {
        var previous = GetSubPanel(_currentCategory);
        if (previous != null)
        {
            previous.OnHide();
            previous.gameObject.SetActive(false);
        }

        _currentCategory = category;

        var panel = GetSubPanel(category);
        if (panel == null)
        {
            Logger.Error($"SettingsPanel: 未绑定分类 {category} 的子面板。", LogTag.UI);
            return;
        }

        panel.gameObject.SetActive(true);
        panel.OnShow();
        RefreshCategoryButtonHighlight(category);
    }

    void HideAllSubPanels()
    {
        if (categories == null) return;
        foreach (var entry in categories)
        {
            if (entry?.subPanel != null)
                entry.subPanel.gameObject.SetActive(false);
        }
    }

    SettingsSubPanelBase GetSubPanel(SettingCategory category)
    {
        if (categories == null) return null;
        foreach (var entry in categories)
        {
            if (entry != null && entry.category == category)
                return entry.subPanel;
        }
        return null;
    }

    void RefreshCategoryButtonHighlight(SettingCategory active)
    {
        if (categories == null) return;
        foreach (var entry in categories)
        {
            if (entry?.categoryButton == null) continue;
            bool selected = entry.category == active;
            var colors = entry.categoryButton.colors;
            colors.normalColor = selected ? new Color(0.85f, 0.95f, 1f) : Color.white;
            entry.categoryButton.colors = colors;
        }
    }

    static string GetCategoryDisplayName(SettingCategory category)
    {
        return category switch
        {
            SettingCategory.Graphics => "图形",
            SettingCategory.Audio => "音频",
            SettingCategory.Controls => "控制",
            _ => category.ToString(),
        };
    }
}
