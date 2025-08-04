using System.Text.Json.Serialization;
using PixivCS.Api;

namespace PixivCS.Models.Common;

/// <summary>
/// Pixiv API 错误信息
/// </summary>
public record PixivError
{
    /// <summary>
    /// 面向用户的错误消息
    /// </summary>
    [JsonPropertyName("user_message")]
    public string UserMessage { get; init; } = "";

    /// <summary>
    /// 技术错误消息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = "";

    /// <summary>
    /// 错误原因
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "";

    /// <summary>
    /// 用户消息详情，包含附加错误信息
    /// </summary>
    [JsonPropertyName("user_message_details")]
    public Dictionary<string, object> UserMessageDetails { get; init; } = [];
}

/// <summary>
/// API 响应基类
/// </summary>
public record BaseResponse
{
    /// <summary>
    /// 错误信息，为null时表示请求成功
    /// </summary>
    [JsonPropertyName("error")]
    public PixivError? Error { get; init; }

    /// <summary>
    /// 检查响应是否包含错误
    /// </summary>
    public bool HasError => Error != null;

    /// <summary>
    /// 获取错误消息
    /// </summary>
    public string GetErrorMessage()
    {
        if (Error == null) return string.Empty;
        
        // 优先返回用户消息，其次是技术消息
        if (!string.IsNullOrEmpty(Error.UserMessage))
            return Error.UserMessage;
        if (!string.IsNullOrEmpty(Error.Message))
            return Error.Message;
        if (!string.IsNullOrEmpty(Error.Reason))
            return Error.Reason;
            
        return "未知错误";
    }
}


/// <summary>
/// 带分页的响应基类
/// </summary>
public abstract record PaginatedResponse : BaseResponse
{
    /// <summary>
    /// 下一页的URL，为null或空字符串时表示没有更多数据
    /// </summary>
    [JsonPropertyName("next_url")]
    public string? NextUrl { get; init; }
    
    /// <summary>
    /// 检查是否有下一页
    /// </summary>
    public bool HasNextPage() => !string.IsNullOrWhiteSpace(NextUrl);
}

/// <summary>
/// 图片 URL 集合
/// </summary>
public record ImageUrls
{
    /// <summary>
    /// 正方形中等尺寸图片URL
    /// </summary>
    [JsonPropertyName("square_medium")]
    public string SquareMedium { get; init; } = "";

    /// <summary>
    /// 中等尺寸图片URL
    /// </summary>
    [JsonPropertyName("medium")]
    public string Medium { get; init; } = "";

    /// <summary>
    /// 大尺寸图片URL
    /// </summary>
    [JsonPropertyName("large")]
    public string Large { get; init; } = "";

    /// <summary>
    /// 原始尺寸图片URL，可能为null
    /// </summary>
    [JsonPropertyName("original")]
    public string? Original { get; init; }
}

/// <summary>
/// 用户头像 URL 集合
/// </summary>
public record ProfileImageUrls
{
    /// <summary>
    /// 用户头像中等尺寸URL
    /// </summary>
    [JsonPropertyName("medium")]
    public string Medium { get; init; } = "";
}

/// <summary>
/// 基础标签信息
/// </summary>
public record Tag
{
    /// <summary>
    /// 标签名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    /// <summary>
    /// 标签的翻译名称，可能为null
    /// </summary>
    [JsonPropertyName("translated_name")]
    public string? TranslatedName { get; init; }
}