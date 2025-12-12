using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SettingCategory
{
    Graphics,
    Audio,
    Controls,
    Other
}

public interface ISettingPanel
{
    void ApplyChanges(); // 可选：立即应用 or 等待“应用”按钮
}

public abstract class SettingSubPanel : UIPanel, ISettingPanel
{
    public virtual void ApplyChanges() { }
}

public class GraphicsPanel : SettingSubPanel
{
    // 图形设置实现
}

public class AudioPanel : SettingSubPanel
{
    // 音频设置实现
}

public class ControlsPanel : SettingSubPanel
{
    // 控制设置实现
}

public class OtherSettingsPanel : SettingSubPanel
{
    // 其他设置实现
}

public class SettingsPanel : UIPanel
{
    [Header("References")]
    [SerializeField] Transform contentArea;
    [SerializeField] GameObject categoryButtonPrefab;
    [SerializeField] Transform categoryListContent;
    [SerializeField] Button returnButton;

    // 面板缓存（避免重复 Instantiate）
    Dictionary<SettingCategory, SettingSubPanel> _panelCache = new();
    SettingCategory _currentCategory = SettingCategory.Graphics;

    // 面板类型映射（可替换为 ScriptableObject 配置）
    static readonly Dictionary<SettingCategory, Type> PanelTypeMap = new()
    {
        { SettingCategory.Graphics, typeof(GraphicsPanel) },
        { SettingCategory.Audio, typeof(AudioPanel) },
        { SettingCategory.Controls, typeof(ControlsPanel) },
        { SettingCategory.Other, typeof(OtherSettingsPanel) }
    };

    public override void Initialize()
    {
        base.Initialize();
        returnButton.onClick.AddListener(() => UIManager.Instance.GoBack());
        CreateCategoryButtons();
        ShowCategory(_currentCategory);
    }

    void CreateCategoryButtons()
    {
        foreach (SettingCategory category in Enum.GetValues(typeof(SettingCategory)))
        {
            var btnObj = Instantiate(categoryButtonPrefab, categoryListContent);
            var btn = btnObj.GetComponent<Button>();
            var text = btnObj.GetComponentInChildren<TMP_Text>();

            text.text = category.ToString();
            var localCat = category; // 避免闭包陷阱
            btn.onClick.AddListener(() => ShowCategory(localCat));
        }
    }

    void ShowCategory(SettingCategory category)
    {
        // 隐藏当前面板
        if (_panelCache.TryGetValue(_currentCategory, out var current))
        {
            current.OnHide();
        }

        _currentCategory = category;

        // 获取或创建新面板
        if (!_panelCache.TryGetValue(category, out var panel))
        {
            if (!PanelTypeMap.TryGetValue(category, out Type panelType))
                return;

            var prefab = Resources.Load<GameObject>($"UI/Settings/{panelType.Name}");
            if (prefab == null)
            {
                Debug.LogError($"Missing prefab for {panelType.Name}");
                return;
            }

            var instance = Instantiate(prefab, contentArea);
            panel = instance.GetComponent<SettingSubPanel>();
            panel.Initialize();
            _panelCache[category] = panel;
        }

        panel.OnShow();
    }

    // 可选：全局“应用”按钮调用
    public void OnApplyAllClicked()
    {
        foreach (var panel in _panelCache.Values)
        {
            panel.ApplyChanges();
        }
        // 可触发事件：OnSettingsApplied
    }

    public override void OnHide()
    {
        base.OnHide();
        // 可选择是否销毁缓存（通常保留以加速下次打开）
    }
}