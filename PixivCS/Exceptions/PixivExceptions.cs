namespace PixivCS.Exceptions;

/// <summary>
/// Pixiv API 基础异常
/// </summary>
public class PixivException : Exception
{
    /// <summary>
    /// 初始化 PixivException 类的新实例
    /// </summary>
    public PixivException() { }
    /// <summary>
    /// 使用指定的错误消息初始化 PixivException 类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    public PixivException(string message) : base(message) { }
    /// <summary>
    /// 使用指定的错误消息和内部异常初始化 PixivException 类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public PixivException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 认证相关异常
/// </summary>
public class PixivAuthException : PixivException
{
    /// <summary>
    /// 初始化 PixivAuthException 类的新实例
    /// </summary>
    public PixivAuthException() { }
    /// <summary>
    /// 使用指定的错误消息初始化 PixivAuthException 类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    public PixivAuthException(string message) : base(message) { }
    /// <summary>
    /// 使用指定的错误消息和内部异常初始化 PixivAuthException 类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public PixivAuthException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 网络连接异常
/// </summary>
public class PixivNetworkException : PixivException
{
    /// <summary>
    /// 初始化 PixivNetworkException 类的新实例
    /// </summary>
    public PixivNetworkException() { }
    /// <summary>
    /// 使用指定的错误消息初始化 PixivNetworkException 类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    public PixivNetworkException(string message) : base(message) { }
    /// <summary>
    /// 使用指定的错误消息和内部异常初始化 PixivNetworkException 类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public PixivNetworkException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 频率限制异常
/// </summary>
public class PixivRateLimitException(string message, TimeSpan retryAfter) : PixivException(message)
{
    /// <summary>
    /// 重试等待时间
    /// </summary>
    public TimeSpan RetryAfter { get; } = retryAfter;

    /// <summary>
    /// 使用指定的重试等待时间初始化 PixivRateLimitException 类的新实例
    /// </summary>
    /// <param name="retryAfter">重试等待时间</param>
    public PixivRateLimitException(TimeSpan retryAfter) : this($"Rate limit exceeded. Retry after {retryAfter.TotalSeconds} seconds.", retryAfter)
    {
    }
}

/// <summary>
/// API 响应解析异常
/// </summary>
public class PixivParseException : PixivException
{
    /// <summary>
    /// 导致解析异常的响应内容
    /// </summary>
    public string? ResponseContent { get; }

    /// <summary>
    /// 使用响应内容初始化 PixivParseException 类的新实例
    /// </summary>
    /// <param name="responseContent">响应内容</param>
    public PixivParseException(string? responseContent) : base("Failed to parse API response.")
    {
        ResponseContent = responseContent;
    }

    /// <summary>
    /// 使用指定的错误消息和响应内容初始化 PixivParseException 类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="responseContent">响应内容</param>
    public PixivParseException(string message, string? responseContent) : base(message)
    {
        ResponseContent = responseContent;
    }

    /// <summary>
    /// 使用指定的错误消息、响应内容和内部异常初始化 PixivParseException 类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="responseContent">响应内容</param>
    /// <param name="innerException">内部异常</param>
    public PixivParseException(string message, string? responseContent, Exception innerException) : base(message, innerException)
    {
        ResponseContent = responseContent;
    }
}