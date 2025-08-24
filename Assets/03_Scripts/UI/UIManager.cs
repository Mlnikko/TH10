using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public enum E_Panel
{
    NULL,
    Title,
    RankSelect,
    CharacterSelect,
    WeaponSelect,
    BattlePanel,
    Loading,
}
public class UIManager : Singleton<UIManager>
{
    const string panelPrefabLable = "Panel";
    Dictionary<E_Panel, UnityEngine.GameObject> panelPrefabDict = new();
    Dictionary<E_Panel, BasePanel> panelCachesDict = new();

    public void LoadPrefabRefrence()
    {
        panelPrefabDict.Clear();
        panelCachesDict.Clear();

        var locationsHandle = Addressables.LoadResourceLocationsAsync(panelPrefabLable);
        locationsHandle.WaitForCompletion(); // 同步等待

        List<IResourceLocation> locations = (List<IResourceLocation>)locationsHandle.Result;

        foreach (var location in locations)
        {
            // 根据资源类型加载
            if (location.ResourceType == typeof(UnityEngine.GameObject))
            {
                LoadGameObjectSync(location);
            }
            // 可以添加其他资源类型的处理
            else
            {
                Debug.LogWarning($"Unsupported resource EnemyTypr: {location.ResourceType} at {location.PrimaryKey}");
            }
        }
    }

    void LoadGameObjectSync(IResourceLocation location)
    {
        // 同步加载 Entity
        var handle = Addressables.LoadAssetAsync<UnityEngine.GameObject>(location);
        handle.WaitForCompletion();

        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
        {
            E_Panel panelName = handle.Result.GetComponent<BasePanel>().PanelName;
            panelPrefabDict.Add(panelName, handle.Result);
            Debug.Log($"Successfully loaded and instantiated: {location.PrimaryKey}");
        }
        else
        {
            Debug.LogError($"Failed to load asset: {location.PrimaryKey}");
        }

        // 注意：这里不释放 handle，因为我们需要保持资源加载
    }

    public BasePanel OpenPanel(E_Panel panelName, Transform parent = null, bool isEnable = true)
    {
        // 检查缓存中是否存在面板物体
        if (panelCachesDict.TryGetValue(panelName, out BasePanel panel))
        {
            panel.EnablePanel(isEnable);
            return panel;
        }
        else if (panelPrefabDict.TryGetValue(panelName, out UnityEngine.GameObject panelPrefab))
        {
            UnityEngine.GameObject panelObj = null;
            if (parent != null)
            {
                panelObj = UnityEngine.GameObject.Instantiate(panelPrefab, parent);
            }
            else
            {
                panelObj = UnityEngine.GameObject.Instantiate(panelPrefab, UnityEngine.GameObject.FindGameObjectWithTag("DefaultCanvas").transform);
            }

            BasePanel basePanel = panelObj.GetComponent<BasePanel>();
            if (panelCachesDict.TryAdd(panelName, basePanel))
            {
                basePanel.EnablePanel(isEnable);
            }
            return basePanel;
        }
        return null;
    }

    public BasePanel EnablePanel(E_Panel panelName, bool isEnable)
    {
        if(panelCachesDict.TryGetValue(panelName, out BasePanel panel))
        {
            panel.EnablePanel(isEnable);
            return panel;
        }
        return null;
    }

    public void DestroyPanel(E_Panel panelName, bool instant)
    {
        if(panelCachesDict.TryGetValue(panelName, out BasePanel panel))
        {
            if(instant)
            {
                panelCachesDict.Remove(panelName);
                panel.DestroyPanel(instant);
            }
        }
    }

    public BasePanel GetPanelScript(E_Panel panelName)
    {
        if (panelCachesDict.TryGetValue(panelName,out BasePanel panel))
        {
            return panel;
        }
        return null;
    }
}
