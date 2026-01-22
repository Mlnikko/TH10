using System.Threading.Tasks;
using UnityEngine;

public static class PrefabManager
{
    /// <summary>
    /// 异步加载预制体（自动缓存）
    /// </summary>
    public static Task<GameObject> LoadPrefabAsync(string prefabKey)
    {
        return ResManager.LoadAsync<GameObject>(prefabKey);
    }

    /// <summary>
    /// 批量预加载预制体（如角色、子弹、敌人等）
    /// </summary>
    public static async Task PreloadPrefabsAsync(params string[] prefabKeys)
    {
        await ResManager.PreloadAsync<GameObject>(prefabKeys);
    }

    /// <summary>
    /// 同步获取已加载的预制体（仅用于确定已预加载的场景）
    /// </summary>
    public static GameObject GetPrefab(string prefabKey)
    {
        return ResManager.Get<GameObject>(prefabKey);
    }

    /// <summary>
    /// 实例化预制体（自动加载若未缓存）
    /// 注意：此方法会触发异步加载，需 await！
    /// </summary>
    public static async Task<GameObject> InstantiateAsync(string prefabKey, Transform parent = null, bool worldPositionStays = false)
    {
        var prefab = await LoadPrefabAsync(prefabKey);
        if (prefab == null)
        {
            Logger.Error($"Failed to instantiate: {prefabKey} (prefab is null)", LogTag.Resource);
            return null;
        }
        return Object.Instantiate(prefab, parent, worldPositionStays);
    }

    /// <summary>
    /// 实例化已加载的预制体（同步，不触发加载）
    /// 若未加载则返回 null
    /// </summary>
    public static GameObject Instantiate(string prefabKey, Transform parent = null, bool worldPositionStays = false)
    {
        var prefab = GetPrefab(prefabKey);
        if (prefab == null)
        {
            Logger.Warn($"Prefab not loaded yet: {prefabKey}. Use InstantiateAsync instead.", LogTag.Resource);
            return null;
        }
        return Object.Instantiate(prefab, parent, worldPositionStays);
    }

    // 可选：提供带位置/旋转的重载
    public static async Task<GameObject> InstantiateAsync(string prefabKey, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var go = await InstantiateAsync(prefabKey, parent);
        if (go != null)
        {
            go.transform.SetPositionAndRotation(position, rotation);
        }
        return go;
    }
}