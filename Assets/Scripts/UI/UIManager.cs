using System;
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
                scaler.referenceResolution = new Vector2(1280, 960);
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
    /// <summary>同类型面板尚未完成的打开任务，用于合并并发 ShowPanelAsync，避免重复 Instantiate。</summary>
    readonly Dictionary<string, Task<UIPanel>> _inflightPanelOpens = new();
    readonly object _panelOpenSync = new();

    UIPanelRegistry _registryCachedForLookup;
    Dictionary<string, UIPanelRegistryEntry> _registryEntryByPanelKey;

    UIPanelRegistry ResolveRegistry()
    {
        try
        {
            return ResManager.Instance?.Manifest?.uiPanelRegistry;
        }
        catch
        {
            return null;
        }
    }

    void EnsureRegistryLookup()
    {
        var reg = ResolveRegistry();
        if (ReferenceEquals(reg, _registryCachedForLookup) && _registryEntryByPanelKey != null)
            return;
        _registryCachedForLookup = reg;
        _registryEntryByPanelKey = new Dictionary<string, UIPanelRegistryEntry>(StringComparer.Ordinal);
        if (reg?.entries == null)
            return;
        foreach (var e in reg.entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.panelScriptTypeName))
                continue;
            _registryEntryByPanelKey[e.panelScriptTypeName.Trim()] = e;
        }
    }

    bool TryGetRegistryEntry(string panelKey, out UIPanelRegistryEntry entry)
    {
        EnsureRegistryLookup();
        entry = null;
        return _registryEntryByPanelKey != null && _registryEntryByPanelKey.TryGetValue(panelKey, out entry);
    }

    /// <summary>
    /// 显式 <paramref name="prefabResourceIdOverride"/> 优先；否则使用 Manifest 注册表中的 prefab id；再否则使用面板类名。
    /// </summary>
    string ResolvePrefabLoadId(string panelKey, string prefabResourceIdOverride)
    {
        if (!string.IsNullOrEmpty(prefabResourceIdOverride))
            return StringHelper.NormalizeResourceId(prefabResourceIdOverride);
        if (TryGetRegistryEntry(panelKey, out var e) && !string.IsNullOrEmpty(e.prefabResourceId))
            return StringHelper.NormalizeResourceId(e.prefabResourceId);
        return StringHelper.NormalizeResourceId(panelKey);
    }

    void ApplyPresentationPolicy(UIPanel panel, string panelKey)
    {
        if (panel == null)
            return;
        if (TryGetRegistryEntry(panelKey, out var e) && e.exclusiveFullscreen)
            panel.transform.SetAsLastSibling();
    }

    bool ShouldDestroyInstanceWhenClosed(string panelKey)
    {
        if (!TryGetRegistryEntry(panelKey, out var e))
            return true;
        return e.destroyInstanceWhenClosed;
    }

    void RemovePanelFromStack(UIPanel panel)
    {
        if (panel == null || panelStack.Count == 0)
            return;
        var tempStack = new Stack<UIPanel>();
        while (panelStack.Count > 0)
        {
            var top = panelStack.Pop();
            if (top != panel)
                tempStack.Push(top);
        }
        while (tempStack.Count > 0)
            panelStack.Push(tempStack.Pop());
    }

    /// <summary>
    /// 异步显示面板（返回 Task，支持 await）
    /// </summary>
    /// <param name="prefabResourceId">
    /// Addressables 资源 id（不含 prefab_ 前缀）；若为空则使用 <see cref="GameResourceManifest.uiPanelRegistry"/> 中的配置，
    /// 再无则与面板类型名一致（经 Addressables 键规则小写化）。
    /// </param>
    public async Task<T> ShowPanelAsync<T>(object data = null, string prefabResourceId = null) where T : UIPanel
    {
        string panelKey = typeof(T).Name;

        lock (_panelOpenSync)
        {
            // 1. 已缓存（含仅隐藏）：直接显示，避免重复 Addressables 加载
            if (activePanels.TryGetValue(panelKey, out var cached) && cached != null)
            {
                cached.gameObject.SetActive(true);
                cached.OnShow(data);
                PushToStack(cached);
                ApplyPresentationPolicy(cached, panelKey);
                return (T)cached;
            }
        }

        Task<UIPanel> openTask;
        bool iRegisteredInflight = false;
        lock (_panelOpenSync)
        {
            if (_inflightPanelOpens.TryGetValue(panelKey, out var existingTask))
                openTask = existingTask;
            else
            {
                openTask = OpenPanelTaskAsync<T>(data, prefabResourceId, panelKey);
                _inflightPanelOpens[panelKey] = openTask;
                iRegisteredInflight = true;
            }
        }

        try
        {
            UIPanel result = await openTask;
            return result as T;
        }
        finally
        {
            if (iRegisteredInflight)
            {
                lock (_panelOpenSync)
                {
                    // 仅注册方在任务结束后移除；其它等待方共享同一 Task
                    if (_inflightPanelOpens.TryGetValue(panelKey, out var t) && ReferenceEquals(t, openTask))
                        _inflightPanelOpens.Remove(panelKey);
                }
            }
        }
    }

    async Task<UIPanel> OpenPanelTaskAsync<T>(object data, string prefabResourceId, string panelKey) where T : UIPanel
    {
        if (ResManager.Instance == null)
        {
            Logger.Error("[UIManager] ResManager.Instance 为空，无法加载面板预制体。", LogTag.UI);
            return null;
        }

        string loadId = ResolvePrefabLoadId(panelKey, prefabResourceId);
        GameObject prefab = await ResManager.Instance.LoadAsync<GameObject>(E_ResourceCategory.Prefab, loadId);
        if (prefab == null)
        {
            Logger.Error($"[UIManager] 预制体加载结果为 null：prefab_{loadId}", LogTag.UI);
            return null;
        }

        Transform canvasTransform = Canvas != null ? Canvas.transform : null;
        if (canvasTransform == null)
        {
            Logger.Error("[UIManager] Canvas 未就绪，无法实例化面板。", LogTag.UI);
            return null;
        }

        lock (_panelOpenSync)
        {
            // 加载期间可能已被其它逻辑放入字典（极少）；再次命中缓存则不再 Instantiate
            if (activePanels.TryGetValue(panelKey, out var cached) && cached != null)
            {
                cached.gameObject.SetActive(true);
                cached.OnShow(data);
                PushToStack(cached);
                ApplyPresentationPolicy(cached, panelKey);
                return cached;
            }
        }

        var shown = InternalShowPanel<T>(prefab, data, canvasTransform);
        ApplyPresentationPolicy(shown, panelKey);
        return shown;
    }

    T InternalShowPanel<T>(GameObject prefab, object data, Transform parent) where T : UIPanel
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
        GameObject go = Instantiate(prefab, parent);
        if (go == null)
        {
            Logger.Error($"[UIManager] Instantiate 失败：{prefab.name}", LogTag.UI);
            return null;
        }

        T panel = go.GetComponent<T>() ?? go.AddComponent<T>();
        if (panel == null)
        {
            Logger.Error($"[UIManager] 面板缺少组件 {typeof(T).Name}：{go.name}", LogTag.UI);
            Destroy(go);
            return null;
        }

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
        if (!activePanels.TryGetValue(name, out var panel) || panel == null)
            return;

        RemovePanelFromStack(panel);

        if (ShouldDestroyInstanceWhenClosed(name))
        {
            Destroy(panel.gameObject);
            activePanels.Remove(name);
        }
        else
        {
            panel.gameObject.SetActive(false);
            panel.OnHide();
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
        lock (_panelOpenSync)
        {
            _inflightPanelOpens.Clear();
        }
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