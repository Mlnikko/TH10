using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableUIResourceLoader : IUIResourceLoader
{
    private Dictionary<string, GameObject> loadedPrefabs = new();
    private Dictionary<string, AsyncOperationHandle<GameObject>> loadingHandles = new();

    public bool IsLoaded(string panelName) => loadedPrefabs.ContainsKey(panelName);

    public GameObject GetPrefab(string panelName) => loadedPrefabs.GetValueOrDefault(panelName);

    public void LoadPrefabAsync(string panelName, Action<GameObject> onComplete)
    {
        if (IsLoaded(panelName))
        {
            onComplete?.Invoke(GetPrefab(panelName));
            return;
        }

        string key = $"UI/Panels/{panelName}"; // 或从配置表读取
        var handle = Addressables.LoadAssetAsync<GameObject>(key);
        loadingHandles[panelName] = handle;

        handle.Completed += op =>
        {
            loadingHandles.Remove(panelName);
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                loadedPrefabs[panelName] = op.Result;
                onComplete?.Invoke(op.Result);
            }
            else
            {
                Debug.LogError($"Addressables load failed: {key}");
                onComplete?.Invoke(null);
            }
        };
    }

    // 可选：提供释放接口（按需）
    public void ReleaseAll()
    {
        foreach (var handle in loadingHandles.Values)
        {
            Addressables.Release(handle);
        }
        loadingHandles.Clear();
        loadedPrefabs.Clear();
    }
}