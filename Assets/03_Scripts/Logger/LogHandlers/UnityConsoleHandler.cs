using System.Collections.Generic;

public class UnityConsoleHandler : ILogHandler
{
    // 配置选项
    public bool EnableColor = true;

    // 日志等级着色
    static readonly Dictionary<LogLevel, string> LevelColors = new()
    {
        { LogLevel.Debug,    "#808080" },  // 灰色
        { LogLevel.Info,     "#FFFFFF" },  // 白色
        { LogLevel.Warning,  "#FFFF00" },  // 黄色
        { LogLevel.Error,    "#FF0000" },  // 红色
        { LogLevel.Critical, "#FF4444" }   // 亮红色
    };

    // 日志标签着色
    static readonly Dictionary<LogTag, string> TagColors = new()
    {
        { LogTag.Net,        "#00FFFF" },  // 青色
        { LogTag.Resource,   "#FFA500" },  // 橙色
        { LogTag.Input,      "#ADD8E6" },  // 浅蓝色
    };

    public void ProcessLog(in LogData logData)
    {
        var logType = logData.Level switch
        {
            LogLevel.Warning => UnityEngine.LogType.Warning,
            LogLevel.Error or LogLevel.Critical => UnityEngine.LogType.Error,
            _ => UnityEngine.LogType.Log
        };

        string message = EnableColor
            ? FormatColoredLog(logData)
            : logData.ToString();

        // 使用 unityLogger 保证与 Unity 控制台行为一致
        UnityEngine.Debug.unityLogger.logHandler.LogFormat(
            logType,
            logData.Context as UnityEngine.Object,
            "{0}",
            message
        );
    }

    static string FormatColoredLog(in LogData logData)
    {
        string timeStr = $"[{logData.Time:yyyy-MM-dd HH:mm:ss.fff}]";

        // 1. 着色日志等级
        string levelStr = logData.Level switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Info => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "LOG"
        };

        string levelColor = LevelColors.GetValueOrDefault(logData.Level, "white");
        string coloredLevel = $"<color={levelColor}>[{levelStr}]</color>";

        // 2. 处理标签
        string tagPart = "";
        if (logData.Tag != LogTag.Misc) // 假设 Misc 是默认
        {
            string tagColor = TagColors.GetValueOrDefault(logData.Tag, "green");
            tagPart = $" <color={tagColor}>[{logData.Tag}]</color>";
        }

        // 3. 拼接完整消息
        return $"{timeStr} {coloredLevel}{tagPart} {logData.Message}";
    }
}