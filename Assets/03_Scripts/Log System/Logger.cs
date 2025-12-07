using System.Collections.Generic;

#region 日志数据结构 LogData

public enum LogLevel
{
    Debug,    // 调试
    Info,     // 信息
    Warning,  // 警告
    Error,    // 错误
    Critical  // 崩溃
}

public enum LogTag
{
    Default,
    UI,
    Network,
    Audio,
    Resource,
    Input,
    Init
}

/// <summary>
/// 日志数据
/// </summary>
public class LogData
{
    #region 日志数据成员变量
    /// <summary>
    /// 日志时间
    /// </summary>
    public System.DateTime Time
    {
        get
        {
            return _time;
        }
    }
    readonly System.DateTime _time;

    /// <summary>
    /// 日志等级
    /// </summary>
    public LogLevel Level
    {
        get
        {
            return _level;
        }
    }
    readonly LogLevel _level;

    /// <summary>
    /// 日志标签
    /// </summary>
    public LogTag Tag
    {
        get
        {
            return _tag;
        }
    }
    readonly LogTag _tag;

    /// <summary>
    /// 日志信息
    /// </summary>
    public string Message
    {
        get
        {
            return _message;
        }
    }
    readonly string _message;

    /// <summary>
    /// 上下文
    /// </summary>
    public object Context
    {
        get
        {
            return _context;
        }
    }
    readonly object _context;
    #endregion

    /// <summary>
    /// 自动记录日志时间的日志构造
    /// </summary>
    /// <param name="level"></param>
    /// <param name="message"></param>
    /// <param name="tag"></param>
    /// <param name="context"></param>
    public LogData(LogLevel level, LogTag tag, string message, object context)
    {
        _time = System.DateTime.Now;
        _level = level;
        _tag = tag;
        _message = message;      
        _context = context;
    }

    public override string ToString()
    {
        if(Tag == LogTag.Default) return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] {Message}";
        return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] [{Tag}] {Message}";
    }
}

#endregion

public static class Logger
{
    static bool _isInitialized = false;
    static readonly List<ILogHandler> _handlers = new();

    public static void Init()
    {
        if (_isInitialized) return;
        _handlers.Clear();
        // 添加默认 handler（如 UnityLogHandler）
        AddLogHandler(new UnityConsoleHandler());
        _isInitialized = true;
    }

    public static void Reset()
    {
        _handlers.Clear();
        _isInitialized = false;
    }

    /// <summary>
    /// 添加日志处理器
    /// </summary>
    /// <param name="handler"></param>
    public static void AddLogHandler(ILogHandler handler)
    {
        if (_handlers.Contains(handler)) return;
        _handlers.Add(handler);
    }

    /// <summary>
    /// 生成游戏日志并分发给日志处理器
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="message">日志信息</param>
    /// <param name="tag">日志标签</param>
    /// <param name="context">上下文</param>
    public static void Log(string message, LogLevel level, LogTag tag = LogTag.Default, object context = null)
    {
        LogData logData = new(level, tag, message, context);
        foreach (ILogHandler handler in _handlers)
        {
            handler.ProcessLog(logData);
        }
    }

    #region 快捷日志方法
    public static void Debug(string message, LogTag tag = LogTag.Default, object context = null)
    {
        Log(message, LogLevel.Debug, tag, context);
    }

    public static void Info(string message, LogTag tag = LogTag.Default, object context = null)
    {
        Log(message, LogLevel.Info, tag, context);
    }

    public static void Warn(string message, LogTag tag = LogTag.Default, object context = null)
    {
        Log(message, LogLevel.Warning, tag, context);
    }

    public static void Error(string message, LogTag tag = LogTag.Default, object context = null)
    {
        Log(message, LogLevel.Error, tag, context);
    }

    public static void Critical(string message, LogTag tag = LogTag.Default, object context = null)
    {
        Log(message, LogLevel.Critical, tag, context);
    }
    #endregion
}
