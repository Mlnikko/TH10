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

        // 如果任务已完成且有异常，立即记录（便于调试与日志聚合）
        if (task.IsFaulted)
        {
            LogTaskFault(task.Exception);
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
            Logger.Error($"[Async] Fire-and-forget 任务异常: {ex.Message}", LogTag.Misc);
            Debug.LogException(ex);
        }
    }

    static void LogTaskFault(AggregateException aggregate)
    {
        Exception ex = aggregate?.GetBaseException() ?? aggregate;
        Logger.Error($"[Async] Fire-and-forget 任务已完成且失败: {ex?.Message ?? aggregate?.ToString()}", LogTag.Misc);
        if (aggregate != null)
            Debug.LogException(aggregate);
        else if (ex != null)
            Debug.LogException(ex);
    }
}