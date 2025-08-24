using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity控制台日志处理器
/// </summary>
public class ConsoleHandler : ILogHandler
{
    public void ProcessLog(LogData log)
    {
        switch (log.Level)
        {
            case LogLevel.Debug:
                Debug.Log(log.Message);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(log.Message);
                break;
            case LogLevel.Error:
                Debug.LogError(log.Message);
                break;
        }     
    }
}
