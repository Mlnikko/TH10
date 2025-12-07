// DefaultUIResourceLoader.cs
using System.Collections.Generic;
using UnityEngine;

public class DefaultUIResourceLoader : IUIResourceLoader
{
    private Dictionary<string, GameObject> loadedPrefabs = new();

    public bool IsLoaded(string panelName) => loadedPrefabs.ContainsKey(panelName);

    public GameObject GetPrefab(string panelName)
    {
        if (loadedPrefabs.TryGetValue(panelName, out var go)) return go;
        // 同步 fallback（不推荐，仅调试）
        go = Resources.Load<GameObject>($"UI/Panels/{panelName}");
        if (go != null) loadedPrefabs[panelName] = go;
        return go;
    }

    public void LoadPrefabAsync(string panelName, System.Action<GameObject> onComplete)
    {
        // 开发期用 Resources 异步模拟
        var request = Resources.LoadAsync<GameObject>($"UI/Panels/{panelName}");
        request.completed += _ =>
        {
            var go = request.asset as GameObject;
            if (go != null) loadedPrefabs[panelName] = go;
            onComplete?.Invoke(go);
        };
    }
}