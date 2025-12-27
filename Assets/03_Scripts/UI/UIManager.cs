using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : SingletonMono<UIManager>
{
    private const string PANEL_KEY_PREFIX = "UI/Panels/";

    public Canvas Canvas
    {
        get
        {
            if (canvas == null)
            {
                var canvasObj = new GameObject("UICanvas");
                canvasObj.SetActive(false);
                canvasObj.transform.SetParent(transform, false);

                canvas = canvasObj.AddComponent<Canvas>();
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                var raycaster = canvasObj.AddComponent<GraphicRaycaster>();

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

                if (FindObjectOfType<EventSystem>() == null)
                {
                    var esObj = new GameObject("EventSystem");
                    esObj.transform.SetParent(transform, false);
                    esObj.AddComponent<EventSystem>();
                    esObj.AddComponent<StandaloneInputModule>();
                }

                canvasObj.SetActive(true);
            }
            return canvas;
        }
    }
    Canvas canvas;

    readonly Stack<UIPanel> panelStack = new();
    readonly Dictionary<string, UIPanel> activePanels = new();

    // 【同步版本】仅当 prefab 已加载时可用
    public T ShowPanel<T>(object data = null) where T : UIPanel
    {
        string assetKey = PANEL_KEY_PREFIX + typeof(T).Name;
        if (ResManager.IsLoaded(assetKey))
        {
            var prefab = ResManager.Get<GameObject>(assetKey);
            return InternalShowPanel<T>(prefab, data);
        }
        else
        {
            Logger.Error($"Panel {assetKey} not loaded! Use ShowPanelAsync.", LogTag.UI);
            return null;
        }
    }

    /// <summary>
    /// 异步显示面板（返回 Task，支持 await）
    /// </summary>
    public async Task<T> ShowPanelAsync<T>(object data = null) where T : UIPanel
    {
        string panelName = typeof(T).Name;
        string assetKey = PANEL_KEY_PREFIX + panelName;

        // 如果已激活，直接返回（不重新实例化）
        if (activePanels.TryGetValue(panelName, out var existing) && existing != null && existing.gameObject.activeSelf)
        {
            return (T)existing;
        }

        // 尝试从 ResManager 获取（已加载）
        GameObject prefab = ResManager.Get<GameObject>(assetKey);

        // 如果未加载，异步加载
        if (prefab == null)
        {
            prefab = await ResManager.LoadAsync<GameObject>(assetKey);
            if (prefab == null)
            {
                Logger.Error($"Failed to load panel: {assetKey}", LogTag.UI);
                return null;
            }
        }

        // 显示面板
        var panel = InternalShowPanel<T>(prefab, data);
        return panel;
    }

    T InternalShowPanel<T>(GameObject prefab, object data) where T : UIPanel
    {
        string name = typeof(T).Name;

        // 复用已存在但被隐藏的面板
        if (activePanels.TryGetValue(name, out var existing) && existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.OnShow(data);
            PushToStack(existing);
            return (T)existing;
        }

        // 创建新实例
        GameObject go = Instantiate(prefab, Canvas.transform);
        T panel = go.GetComponent<T>() ?? go.AddComponent<T>();

        panel.Initialize();
        panel.OnShow(data);
        activePanels[name] = panel;
        PushToStack(panel);

        return panel;
    }

    void PushToStack(UIPanel panel)
    {
        if (panelStack.Count > 0 && panelStack.Peek() == panel) return;
        panelStack.Push(panel);
    }

    public void HidePanel<T>() where T : UIPanel
    {
        string name = typeof(T).Name;
        if (activePanels.TryGetValue(name, out var panel) && panel != null && panel.gameObject.activeSelf)
        {
            panel.gameObject.SetActive(false);
            panel.OnHide();
        }
    }

    public void ClosePanel<T>() where T : UIPanel
    {
        string name = typeof(T).Name;
        if (activePanels.TryGetValue(name, out var panel) && panel != null)
        {
            Destroy(panel.gameObject);
            activePanels.Remove(name);
            // 从栈中移除
            var tempStack = new Stack<UIPanel>();
            while (panelStack.Count > 0)
            {
                var top = panelStack.Pop();
                if (top != panel)
                {
                    tempStack.Push(top);
                }
            }
            while (tempStack.Count > 0)
            {
                panelStack.Push(tempStack.Pop());
            }
        }
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
            previous.OnShow(); // 可扩展：传回退数据
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
    }
}