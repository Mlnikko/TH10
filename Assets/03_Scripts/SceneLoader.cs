using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static event Action OnSceneLoaded;        // 场景加载完成（通用）
    public static event Action<string> OnSceneLoading; // 开始加载某场景（可用于 UI）

    /// <summary>
    /// 异步加载场景（推荐）
    /// </summary>
    public static void LoadScene(string sceneName, Action onLoaded = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is null or empty!");
            onLoaded?.Invoke();
            return;
        }

        // 防止重复加载
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            Debug.LogWarning($"Already in scene: {sceneName}");
            onLoaded?.Invoke();
            return;
        }

        OnSceneLoading?.Invoke(sceneName);
        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = true; // 立即激活（不暂停）

        // 如果需要精细控制（如进度条），可设为 false 并手动激活
        // operation.completed += (op) => { ... };

        if (onLoaded != null)
        {
            operation.completed += _ => onLoaded.Invoke();
        }

        // 触发全局事件
        operation.completed += _ => OnSceneLoaded?.Invoke();
    }

    /// <summary>
    /// 同步加载（仅用于编辑器快速测试，不推荐运行时使用）
    /// </summary>
    public static void LoadSceneImmediate(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        OnSceneLoaded?.Invoke();
    }

    /// <summary>
    /// 获取当前场景名
    /// </summary>
    public static string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
}