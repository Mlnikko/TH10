// AsyncHelper.cs
using System;
using System.Threading.Tasks;
using UnityEngine;

public static class AsyncHelper
{
    /// <summary>
    /// 安全地 fire-and-forget 一个 Task，自动捕获未处理异常。
    /// 避免使用 _ = task; 导致异常静默或 IDE 警告。
    /// </summary>
    public static void Forget(this Task task)
    {
        if (task == null) return;

        // 如果任务已完成且有异常，立即抛出（便于调试）
        if (task.IsFaulted)
        {
            Debug.LogException(task.Exception);
            return;
        }

        // 否则启动一个后台 await 来捕获异常
        AwaitAndLog(task);
    }

    static async void AwaitAndLog(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}