using System.Text.Json.Serialization;
using PixivCS.Models.Common;
using PixivCS.Models.Illust;
using PixivCS.Models.Novel;
using PixivCS.Models.User;

namespace PixivCS.Models.Search;

/// <summary>
/// 插画搜索结果响应
/// </summary>
public record SearchIllustResult : PaginatedResponse
{
    /// <summary>
    /// 搜索到的插画列表
    /// </summary>
    [JsonPropertyName("illusts")]
    public List<IllustInfo>? Illusts { get; init; }

    /// <summary>
    /// 搜索时间范围限制
    /// </summary>
    [JsonPropertyName("search_span_limit")]
    public int SearchSpanLimit { get; init; }

    /// <summary>
    /// 是否显示AI作品
    /// </summary>
    [JsonPropertyName("show_ai")]
    public bool ShowAi { get; init; }
}

/// <summary>
/// 小说搜索结果响应
/// </summary>
public record SearchNovelResult : PaginatedResponse
{
    /// <summary>
    /// 搜索到的小说列表
    /// </summary>
    [JsonPropertyName("novels")]
    public List<NovelInfo>? Novels { get; init; }

    /// <summary>
    /// 搜索时间范围限制
    /// </summary>
    [JsonPropertyName("search_span_limit")]
    public int SearchSpanLimit { get; init; }

    /// <summary>
    /// 是否显示AI作品
    /// </summary>
    [JsonPropertyName("show_ai")]
    public bool ShowAi { get; init; }
}

/// <summary>
/// 用户搜索结果响应
/// </summary>
public record SearchUserResult : PaginatedResponse
{
    /// <summary>
    /// 用户预览列表
    /// </summary>
    [JsonPropertyName("user_previews")]
    public List<UserPreview>? UserPreviews { get; init; }
}

/// <summary>
/// 用户收藏标签响应
/// </summary>
public record UserBookmarkTags : PaginatedResponse
{
    /// <summary>
    /// 用户收藏标签列表
    /// </summary>
    [JsonPropertyName("bookmark_tags")]
    public List<BookmarkTagInfo>? BookmarkTags { get; init; }
}

/// <summary>
/// 收藏标签信息
/// </summary>
public record BookmarkTagInfo
{
    /// <summary>
    /// 标签名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// 标签使用次数
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }
}

/// <summary>
/// 用户列表响应
/// </summary>
public record UserList : PaginatedResponse
{
    /// <summary>
    /// 用户信息列表
    /// </summary>
    [JsonPropertyName("users")]
    public List<UserInfo>? Users { get; init; }
}