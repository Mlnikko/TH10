using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : SingletonMono<UIManager>
{
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

    /// <summary>
    /// 异步显示面板（返回 Task，支持 await）
    /// </summary>
    public async Task<T> ShowPanelAsync<T>(object data = null) where T : UIPanel
    {
        string panelKey = typeof(T).Name;

        // 如果已激活，直接返回（不重新实例化）
        if (activePanels.TryGetValue(panelKey, out var existing) && existing != null && existing.gameObject.activeSelf)
        {
            return (T)existing;
        }

        GameObject prefab = await ResManager.Instance.LoadAsync<GameObject>(E_ResourceCategory.Prefab, panelKey);

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
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }
        activePanels.Clear();
        panelStack.Clear();
    }


    #region 调试面板
    const string UnitTestPanelPrefabName = "UnitTestPanel";
    UIPanel _unitTestPanel;
    GameObject _unitTestPanelObj;
    /// <summary>
    /// 切换调试面板显示/隐藏
    /// </summary>
    public async Task ToggleDebugPanelAsync()
    {
        if (_unitTestPanelObj == null)
        {
            GameObject prefab = await ResManager.Instance.LoadAsync<GameObject>(E_ResourceCategory.Prefab, UnitTestPanelPrefabName);

            if (prefab == null)
            {
                Debug.LogError("[UIManager] UnitTestPanel prefab not found!");
                return;
            }

            // 2. 实例化
            _unitTestPanelObj = Instantiate(prefab, Canvas.transform);
            _unitTestPanelObj.name = "UnitTestPanel_Instance";

            // 3. 获取组件并初始化
            _unitTestPanel = _unitTestPanelObj.GetComponent<UIPanel>();
            if (_unitTestPanel == null)
            {
                _unitTestPanel = _unitTestPanelObj.AddComponent<UnitTestPanel>(); // 确保有脚本
            }

            // 4. 设置层级最高
            _unitTestPanelObj.transform.SetAsLastSibling();

            // 5. 初始化 (不传入 stack，不加入 activePanels 字典)
            _unitTestPanel.Initialize();
            _unitTestPanel.OnShow(null);
            _unitTestPanelObj.SetActive(false); // 默认隐藏，等待切换显示
        }

        // 6. 切换显示状态
        bool isActive = _unitTestPanelObj.activeSelf;
        _unitTestPanelObj.SetActive(!isActive);

        if (!isActive)
        {
            _unitTestPanel.OnShow(null); // 重新显示时刷新数据
        }
        else
        {
            _unitTestPanel.OnHide();
        }
    }

    public void DestroyDebugPanel()
    {
        if (_unitTestPanelObj != null)
        {
            Destroy(_unitTestPanelObj);
            _unitTestPanelObj = null;
            _unitTestPanel = null;
        }
    }
    #endregion
}