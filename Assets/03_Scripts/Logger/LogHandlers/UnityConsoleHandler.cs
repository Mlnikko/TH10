using System.Collections.Generic;

public class UnityConsoleHandler : ILogHandler
{
    // 颜色映射（可配置）- 16进制版本
    static readonly Dictionary<LogLevel, string> LevelColors = new()
    {
        { LogLevel.Debug,    "#808080" },  // 灰色
        { LogLevel.Info,     "#FFFFFF" },  // 白色
        { LogLevel.Warning,  "#FFFF00" },  // 黄色
        { LogLevel.Error,    "#FF0000" },  // 红色
        { LogLevel.Critical, "#FF4444" }   // 亮红色
    };

    // 标签高亮（可选：关键标签额外着色）
    static readonly Dictionary<LogTag, string> TagColors = new()
    {
        { LogTag.Net,        "#00FFFF" },  // 青色
        { LogTag.Resource,   "#FFA500" },  // 橙色
        { LogTag.Input,      "#ADD8E6" },  // 浅蓝色
    };

    public void ProcessLog(in LogData logData)
    {
        string coloredMessage = FormatColoredLog(logData);

        // 转为 UnityEngine.LogType
        var logType = logData.Level switch
        {
            LogLevel.Warning => UnityEngine.LogType.Warning,
            LogLevel.Error or LogLevel.Critical => UnityEngine.LogType.Error,
            _ => UnityEngine.LogType.Log
        };

        UnityEngine.Debug.Log(coloredMessage, logData.Context as UnityEngine.Object);
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