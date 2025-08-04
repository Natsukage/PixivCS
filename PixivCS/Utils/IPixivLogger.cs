namespace PixivCS.Utils;

/// <summary>
/// Pixiv 库日志接口
/// 用户可以实现此接口来自定义日志行为，默认情况下使用 NullPixivLogger（无输出）
/// </summary>
public interface IPixivLogger
{
    /// <summary>
    /// 记录调试信息
    /// </summary>
    void LogDebug(string message);
    
    /// <summary>
    /// 记录一般信息
    /// </summary>
    void LogInfo(string message);
    
    /// <summary>
    /// 记录警告信息
    /// </summary>
    void LogWarning(string message);
    
    /// <summary>
    /// 记录错误信息
    /// </summary>
    void LogError(string message);
    
    /// <summary>
    /// 记录错误信息（包含异常）
    /// </summary>
    void LogError(string message, Exception exception);
}

/// <summary>
/// 默认的空日志记录器 - 不输出任何信息
/// 确保库在默认情况下完全静默工作
/// </summary>
public sealed class NullPixivLogger : IPixivLogger
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static readonly NullPixivLogger Instance = new();
    
    private NullPixivLogger() { }
    
    /// <summary>
    /// 记录调试信息（空实现）
    /// </summary>
    /// <param name="message">日志消息</param>
    public void LogDebug(string message) { }
    /// <summary>
    /// 记录信息（空实现）
    /// </summary>
    /// <param name="message">日志消息</param>
    public void LogInfo(string message) { }
    /// <summary>
    /// 记录警告信息（空实现）
    /// </summary>
    /// <param name="message">日志消息</param>
    public void LogWarning(string message) { }
    /// <summary>
    /// 记录错误信息（空实现）
    /// </summary>
    /// <param name="message">日志消息</param>
    public void LogError(string message) { }
    /// <summary>
    /// 记录错误信息和异常（空实现）
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="exception">异常对象</param>
    public void LogError(string message, Exception exception) { }
}