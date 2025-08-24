using System.Collections.Generic;

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Verbose,  // 开发期细节日志
    Debug,    // 调试信息
    Info,     // 常规信息
    Warning,  // 警告（不影响运行）
    Error,    // 可恢复错误
    Critical  // 崩溃级错误
}

public enum LogTag
{
    None,
    UI,
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
        return string.Format("[{0}] {1} - {2}", _time, _level, _message);
    }
}

public class GameLogger : Singleton<GameLogger>
{
    static readonly List<ILogHandler> _handlers = new();
    public GameLogger()
    {
        AddHandler(new ConsoleHandler());
    }

    /// <summary>
    /// 添加日志处理器
    /// </summary>
    /// <param name="handler"></param>
    public static void AddHandler(ILogHandler handler)
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
    public static void Log(LogLevel level, string message, LogTag tag = LogTag.None, object context = null)
    {
        LogData logData = new(level, tag, message, context);
        foreach (ILogHandler handler in _handlers)
        {
            handler.ProcessLog(logData);
        }
    }

    public static void Debug(string message)
    {
        UnityEngine.Debug.Log(message);
    }
}
