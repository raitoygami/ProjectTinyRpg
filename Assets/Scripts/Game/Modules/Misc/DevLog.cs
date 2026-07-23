using UnityEngine;

/// <summary>
///     开发用日志：通过 <see cref="System.Diagnostics.ConditionalAttribute"/> 绑定 <c>UNITY_EDITOR</c> 与 <c>DEVELOPMENT_BUILD</c>。
///     当二者均未定义时，编译器会移除对输出方法的调用，调用处实参（含字符串插值）不会求值。
///     在编辑器 / Development 构建中，仅当消息等级 <b>不低于</b> <see cref="MinimumLevel"/> 时才真正打印。
/// </summary>
public static class DevLog
{
    /// <summary>数值越大表示越严重；仅 <c>messageLevel &gt;= <see cref="MinimumLevel"/></c> 时输出。</summary>
    public enum LogLevel
    {
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4
    }

    /// <summary>低于该等级的消息将被忽略。默认 <see cref="LogLevel.Verbose"/>，即全部输出。</summary>
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Verbose;

    private static bool ShouldOutput(LogLevel messageLevel) => messageLevel >= MinimumLevel;

    /// <summary>为控制台富文本着色（编辑器 Console 支持 <c>&lt;color&gt;</c>）。</summary>
    private static string WithColor(string message, Color? color)
    {
        if (color == null) return message;
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color.Value)}>{message}</color>";
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context = null, Color? color = null)
    {
        if (!ShouldOutput(LogLevel.Debug)) return;
        message = WithColor(message, color);
        if (context != null)
            Debug.Log(message, context);
        else
            Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogVerbose(string message, Object context = null, Color? color = null)
    {
        if (!ShouldOutput(LogLevel.Verbose)) return;
        message = WithColor(message, color);
        if (context != null)
            Debug.Log(message, context);
        else
            Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogInfo(string message, Object context = null, Color? color = null)
    {
        if (!ShouldOutput(LogLevel.Info)) return;
        message = WithColor(message, color);
        if (context != null)
            Debug.Log(message, context);
        else
            Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, Object context = null, Color? color = null)
    {
        if (!ShouldOutput(LogLevel.Warning)) return;
        message = WithColor(message, color);
        if (context != null)
            Debug.LogWarning(message, context);
        else
            Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message, Object context = null, Color? color = null)
    {
        if (!ShouldOutput(LogLevel.Error)) return;
        message = WithColor(message, color);
        if (context != null)
            Debug.LogError(message, context);
        else
            Debug.LogError(message);
    }

    /// <summary>按指定等级输出；与 <see cref="MinimumLevel"/> 比较规则同其它重载。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Write(LogLevel level, string message, Object context = null, Color? color = null)
    {
        if (!ShouldOutput(level)) return;
        message = WithColor(message, color);
        if (level == LogLevel.Warning)
        {
            if (context != null)
                Debug.LogWarning(message, context);
            else
                Debug.LogWarning(message);
        }
        else if (level == LogLevel.Error)
        {
            if (context != null)
                Debug.LogError(message, context);
            else
                Debug.LogError(message);
        }
        else
        {
            if (context != null)
                Debug.Log(message, context);
            else
                Debug.Log(message);
        }
    }
}
