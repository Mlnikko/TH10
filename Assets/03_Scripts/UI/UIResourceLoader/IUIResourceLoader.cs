using UnityEngine;

public interface IUIResourceLoader
{
    bool IsLoaded(string panelName);
    GameObject GetPrefab(string panelName);
    void LoadPrefabAsync(string panelName, System.Action<GameObject> onComplete);
}