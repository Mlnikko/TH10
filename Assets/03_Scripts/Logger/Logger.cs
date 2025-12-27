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
    Misc,
    UI,
    Net,
    Room,
    Battle,
    Audio,
    Resource,
    Input,
    Config
}

/// <summary>
/// 日志数据
/// </summary>
public readonly struct LogData
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
        string levelStr = Level switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Info => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "!CRT!",
            _ => "LOG"
        };

        if (Tag == LogTag.Misc)
            return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] [{levelStr}] {Message}";
        else
            return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] [{levelStr}] [{Tag}] {Message}";
    }
}

#endregion

public static class Logger
{
    static Logger()
    {
        _handlers = new List<ILogHandler>
        {
            new UnityConsoleHandler() // 默认处理器
        };
    }

    static readonly List<ILogHandler> _handlers;

    /// <summary>
    /// 添加日志处理器
    /// </summary>
    /// <param name="handler"></param>
    public static void AddLogHandler(ILogHandler handler)
    {
        if (handler == null) return;
        lock (_handlers)
        {
            if (!_handlers.Contains(handler))
                _handlers.Add(handler);
        }
    }

    /// <summary>
    /// 生成游戏日志并分发给日志处理器
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="message">日志信息</param>
    /// <param name="tag">日志标签</param>
    /// <param name="context">上下文</param>
    public static void Log(string message, LogLevel level, LogTag tag = LogTag.Misc, object context = null)
    {
        var logData = new LogData(level, tag, message, context);
        ILogHandler[] snapshot;
        lock (_handlers)
        {
            snapshot = _handlers.ToArray();
        }

        foreach (var handler in snapshot)
        {
            try
            {
                handler?.ProcessLog(logData);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logger handler error: {ex}");
            }
        }
    }

    #region 快捷日志方法

    public static void Debug(string m, LogTag t = LogTag.Misc, object c = null) => Log(m, LogLevel.Debug, t, c);
    public static void Info(string m, LogTag t = LogTag.Misc, object c = null) => Log(m, LogLevel.Info, t, c);
    public static void Warn(string m, LogTag t = LogTag.Misc, object c = null) => Log(m, LogLevel.Warning, t, c);
    public static void Error(string m, LogTag t = LogTag.Misc, object c = null) => Log(m, LogLevel.Error, t, c);
    public static void Critical(string m, LogTag t = LogTag.Misc, object c = null) => Log(m, LogLevel.Critical, t, c);
    public static void Exception(System.Exception ex, LogTag tag = LogTag.Misc, object context = null)
    {
        if (ex == null) return;
        string msg = $"[{ex.GetType().Name}] {ex.Message}\n{ex.StackTrace}";
        Log(msg, LogLevel.Error, tag, context ?? ex);
    }

    #endregion
}
