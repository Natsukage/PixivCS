namespace PixivCS.Utils;

/// <summary>
/// 简单的控制台日志记录器实现（仅供示例参考）
/// 用户可以参考此实现来创建自己的日志记录器
/// </summary>
public class ConsolePixivLogger : IPixivLogger
{
    private readonly bool _enableDebug;

    /// <summary>
    /// 初始化控制台日志记录器
    /// </summary>
    /// <param name="enableDebug">是否启用调试日志</param>
    public ConsolePixivLogger(bool enableDebug = false)
    {
        _enableDebug = enableDebug;
    }

    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void LogDebug(string message)
    {
        if (_enableDebug)
        {
            Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} {message}");
        }
    }

    /// <summary>
    /// 记录信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void LogInfo(string message)
    {
        Console.WriteLine($"[INFO]  {DateTime.Now:HH:mm:ss} {message}");
    }

    /// <summary>
    /// 记录警告信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void LogWarning(string message)
    {
        Console.WriteLine($"[WARN]  {DateTime.Now:HH:mm:ss} {message}");
    }

    /// <summary>
    /// 记录错误信息
    /// </summary>
    /// <param name="message">日志消息</param>
    public void LogError(string message)
    {
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}");
    }

    /// <summary>
    /// 记录错误信息和异常
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    public void LogError(string message, Exception exception)
    {
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}");
        Console.WriteLine($"        异常: {exception.Message}");
    }
}