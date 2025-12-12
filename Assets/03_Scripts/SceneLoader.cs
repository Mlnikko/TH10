using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AsyncOperationExtensions
{
    public static Awaiter GetAwaiter(this AsyncOperation asyncOp)
    {
        return new Awaiter(asyncOp);
    }

    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly AsyncOperation _asyncOp;

        public Awaiter(AsyncOperation asyncOp)
        {
            _asyncOp = asyncOp;
        }

        public bool IsCompleted => _asyncOp.isDone;

        public void GetResult()
        {
            // 可选：这里可以检查是否有错误（但 Unity 不抛异常）
            // 直接返回即可
        }

        public void OnCompleted(Action continuation)
        {
            _asyncOp.completed += _ => continuation();
        }
    }
}

public static class SceneLoader
{
    public static event Action OnSceneLoaded;
    public static event Action<string> OnSceneLoading;

    // ====== 【新】Task 式异步加载（推荐使用） ======
    public static async Task<bool> LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Logger.Error("Scene name is null or empty!");
            return false;
        }

        // 如果已在目标场景，直接返回成功
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            return true;
        }

        OnSceneLoading?.Invoke(sceneName);

        try
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = true;

            // 等待场景加载完成
            await op;

            // 验证场景是否真正加载成功
            bool success = SceneManager.GetSceneByName(sceneName).IsValid() &&
                           SceneManager.GetSceneByName(sceneName).isLoaded;

            if (success)
            {
                OnSceneLoaded?.Invoke();
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load scene '{sceneName}': {ex}");
            return false;
        }
    }

    // ====== 【保留】旧回调式（用于兼容） ======
    public static void LoadScene(string sceneName, Action<bool> onLoaded = null)
    {
        // 委托给新的 async 方法，并在完成后调用回调
        LoadSceneAndInvokeCallback(sceneName, onloaded: onLoaded);
    }

    // 辅助方法：避免在 LoadScene 中直接使用 async void（便于异常追踪）
    static async void LoadSceneAndInvokeCallback(string sceneName, Action<bool> onloaded)
    {
        try
        {
            bool result = await LoadSceneAsync(sceneName);
            onloaded?.Invoke(result);
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception in LoadScene callback wrapper: {ex}");
            onloaded?.Invoke(false);
        }
    }

    // ====== 工具方法 ======
    public static string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
}