using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : SingletonMono<UIManager>
{
    private IUIResourceLoader resourceLoader;
    private Dictionary<string, GameObject> prefabCache = new();
    private Stack<UIPanel> panelStack = new();
    private Dictionary<string, UIPanel> activePanels = new();

    // 可外部注入（热更系统初始化时调用）
    public void Initialize(IUIResourceLoader loader)
    {
        this.resourceLoader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    protected override void OnSingletonInit()
    {
        // 默认使用 Resources（仅开发）
        if (resourceLoader == null)
        {
#if UNITY_EDITOR
            Logger.Warn("UIManager using DefaultUIResourceLoader (Resources). Replace for hot update!", LogTag.UI);
#endif
            resourceLoader = new DefaultUIResourceLoader();
        }
    }

    // 【同步版本】仅当 prefab 已加载时可用（适合已预加载的面板）
    public T ShowPanel<T>(object data = null) where T : UIPanel
    {
        string name = typeof(T).Name;
        if (resourceLoader.IsLoaded(name))
        {
            var prefab = resourceLoader.GetPrefab(name);
            return InternalShowPanel<T>(prefab, data);
        }
        else
        {
            Logger.Error($"Panel {name} not loaded! Use ShowPanelAsync.", LogTag.UI);
            return null;
        }
    }

    /// <summary>
    /// 隐藏指定类型的面板（不从栈中移除，仅禁用 GameObject）
    /// </summary>
    public void HidePanel<T>() where T : UIPanel
    {
        string name = typeof(T).Name;
        if (activePanels.TryGetValue(name, out var panel) && panel != null)
        {
            // 只有激活状态才需要隐藏
            if (panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(false);
                panel.OnHide();
            }

            // 注意：不从 panelStack 中移除！
            // 这样 GoBack() 仍能正确返回
        }
    }

    // 【推荐】异步显示面板（支持热更）
    public void ShowPanelAsync<T>(object data = null, Action<T> onShown = null) where T : UIPanel
    {
        string name = typeof(T).Name;

        // 如果已激活，直接回调
        if (activePanels.TryGetValue(name, out var existing) && existing != null && existing.gameObject.activeSelf)
        {
            onShown?.Invoke((T)existing);
            return;
        }

        // 如果 prefab 已缓存，直接显示
        if (prefabCache.TryGetValue(name, out var cachedPrefab))
        {
            var panel = InternalShowPanel<T>(cachedPrefab, data);
            onShown?.Invoke(panel);
            return;
        }

        // 否则异步加载
        resourceLoader.LoadPrefabAsync(name, (prefab) =>
        {
            if (prefab == null)
            {
                Logger.Error($"Failed to load panel: {name}", LogTag.UI);
                onShown?.Invoke(null);
                return;
            }

            prefabCache[name] = prefab;
            var panel = InternalShowPanel<T>(prefab, data);
            onShown?.Invoke(panel);
        });
    }

    private T InternalShowPanel<T>(GameObject prefab, object data) where T : UIPanel
    {
        string name = typeof(T).Name;

        // 复用已存在但未激活的实例
        if (activePanels.TryGetValue(name, out var existing) && existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.OnShow(data);
            PushToStack(existing);
            return (T)existing;
        }

        // 实例化新面板
        GameObject go = Instantiate(prefab, transform);
        UIPanel panel = go.GetComponent<UIPanel>() ?? go.AddComponent<T>();

        panel.Initialize();
        panel.OnShow(data);
        activePanels[name] = panel;
        PushToStack(panel);

        return (T)panel;
    }

    private void PushToStack(UIPanel panel)
    {
        if (panelStack.Count > 0 && panelStack.Peek() == panel) return;
        panelStack.Push(panel);
    }

    public void GoBack()
    {
        if (panelStack.Count <= 1) return;

        var current = panelStack.Pop();
        current?.gameObject.SetActive(false);
        current?.OnHide();

        var previous = panelStack.Peek();
        if (previous != null)
        {
            previous.gameObject.SetActive(true);
            previous.OnShow(); // 可选：是否需要传回退数据？
        }
    }

    public void CloseAll()
    {
        foreach (var panel in activePanels.Values)
        {
            if (panel != null) Destroy(panel.gameObject);
        }
        activePanels.Clear();
        panelStack.Clear();
        prefabCache.Clear(); // 或选择不清，保留 prefab 引用
    }
}