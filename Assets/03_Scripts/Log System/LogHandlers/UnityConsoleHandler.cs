using UnityEngine;

/// <summary>
/// Unity控制台日志处理器
/// </summary>
public class UnityConsoleHandler : ILogHandler
{
    public void ProcessLog(LogData log)
    {
        switch (log.Level)
        {
            case LogLevel.Debug:
            case LogLevel.Info:
                Debug.Log(log.ToString());
                break;
            case LogLevel.Warning:
                Debug.LogWarning(log.ToString());
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Debug.LogError(log.ToString());
                break;
        }
    }
}
