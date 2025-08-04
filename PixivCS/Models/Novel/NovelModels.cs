using System.Text.Json.Serialization;
using PixivCS.Models.Common;
using PixivCS.Models.User;
using PixivCS.Models.Illust;
using PixivCS.Utils;

namespace PixivCS.Models.Novel;

/// <summary>
/// 小说基础信息
/// </summary>
public record NovelInfo
{
    /// <summary>
    /// 小说ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// 小说标题
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// 小说描述文字
    /// </summary>
    [JsonPropertyName("caption")]
    public required string Caption { get; init; }

    /// <summary>
    /// 限制级别
    /// </summary>
    [JsonPropertyName("restrict")]
    public int Restrict { get; init; }

    /// <summary>
    /// X级限制标识
    /// </summary>
    [JsonPropertyName("x_restrict")]
    public int XRestrict { get; init; }

    /// <summary>
    /// 是否原创作品
    /// </summary>
    [JsonPropertyName("is_original")]
    public bool IsOriginal { get; init; }

    /// <summary>
    /// 小说封面图片URL信息
    /// </summary>
    [JsonPropertyName("image_urls")]
    public required ImageUrls ImageUrls { get; init; }

    /// <summary>
    /// 小说创建时间
    /// </summary>
    [JsonPropertyName("create_date")]
    [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
    public required DateTimeOffset CreateDate { get; init; }

    /// <summary>
    /// 小说标签列表
    /// </summary>
    [JsonPropertyName("tags")]
    public required List<NovelTag> Tags { get; init; }

    /// <summary>
    /// 页面数量
    /// </summary>
    [JsonPropertyName("page_count")]
    public int PageCount { get; init; }

    /// <summary>
    /// 文本长度（字符数）
    /// </summary>
    [JsonPropertyName("text_length")]
    public int TextLength { get; init; }

    /// <summary>
    /// 小说作者信息
    /// </summary>
    [JsonPropertyName("user")]
    public required UserInfo User { get; init; }

    /// <summary>
    /// 小说系列信息
    /// </summary>
    [JsonPropertyName("series")]
    public NovelSeries? Series { get; init; } // 可能为 EmptyObject，此处简化为 null

    /// <summary>
    /// 是否已收藏
    /// </summary>
    [JsonPropertyName("is_bookmarked")]
    public bool IsBookmarked { get; init; }

    /// <summary>
    /// 总收藏数
    /// </summary>
    [JsonPropertyName("total_bookmarks")]
    public int TotalBookmarks { get; init; }

    /// <summary>
    /// 总浏览数
    /// </summary>
    [JsonPropertyName("total_view")]
    public int TotalView { get; init; }

    /// <summary>
    /// 是否可见
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; init; }

    /// <summary>
    /// 总评论数
    /// </summary>
    [JsonPropertyName("total_comments")]
    public int TotalComments { get; init; }

    /// <summary>
    /// 是否被屏蔽
    /// </summary>
    [JsonPropertyName("is_muted")]
    public bool IsMuted { get; init; }

    /// <summary>
    /// 是否仅限好P友可见
    /// </summary>
    [JsonPropertyName("is_mypixiv_only")]
    public bool IsMyPixivOnly { get; init; }

    /// <summary>
    /// 是否为X级限制内容
    /// </summary>
    [JsonPropertyName("is_x_restricted")]
    public bool IsXRestricted { get; init; }

    /// <summary>
    /// AI小说类型标识
    /// </summary>
    [JsonPropertyName("novel_ai_type")]
    public int NovelAiType { get; init; }

    /// <summary>
    /// 评论访问控制设置
    /// </summary>
    [JsonPropertyName("comment_access_control")]
    public int? CommentAccessControl { get; init; } // 可能为 null

    /// <summary>
    /// 小说请求信息
    /// </summary>
    [JsonPropertyName("request")]
    public object? Request { get; init; } // 小说请求信息，可能为 null
}

/// <summary>
/// 小说标签
/// </summary>
public record NovelTag : Tag
{
    /// <summary>
    /// 是否为上传者添加的标签
    /// </summary>
    [JsonPropertyName("added_by_uploaded_user")]
    public bool AddedByUploadedUser { get; init; }
}

/// <summary>
/// 小说系列信息
/// </summary>
public record NovelSeries
{
    /// <summary>
    /// 系列ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 系列标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

/// <summary>
/// 小说详情响应
/// </summary>
public record NovelDetail : BaseResponse
{
    /// <summary>
    /// 小说信息
    /// </summary>
    [JsonPropertyName("novel")]
    public NovelInfo? Novel { get; init; }
}

/// <summary>
/// 小说列表响应
/// </summary>
public record NovelList : PaginatedResponse
{
    /// <summary>
    /// 小说列表
    /// </summary>
    [JsonPropertyName("novels")]
    public List<NovelInfo>? Novels { get; init; }
}

/// <summary>
/// 用户小说列表响应
/// </summary>
public record UserNovels : NovelList;

/// <summary>
/// 推荐小说响应
/// </summary>
public record NovelRecommended : PaginatedResponse
{
    /// <summary>
    /// 推荐小说列表
    /// </summary>
    [JsonPropertyName("novels")]
    public List<NovelInfo>? Novels { get; init; }

    /// <summary>
    /// 排行榜小说列表
    /// </summary>
    [JsonPropertyName("ranking_novels")]
    public List<NovelInfo>? RankingNovels { get; init; }

    /// <summary>
    /// 是否存在竞赛
    /// </summary>
    [JsonPropertyName("contest_exists")]
    public bool ContestExists { get; init; }

    /// <summary>
    /// 隐私政策信息
    /// </summary>
    [JsonPropertyName("privacy_policy")]
    public object? PrivacyPolicy { get; init; }
}

/// <summary>
/// 小说评论响应
/// </summary>
public record NovelComments : PaginatedResponse
{
    /// <summary>
    /// 总评论数
    /// </summary>
    [JsonPropertyName("total_comments")]
    public int TotalComments { get; init; }

    /// <summary>
    /// 评论列表
    /// </summary>
    [JsonPropertyName("comments")]
    public required List<Comment> Comments { get; init; }

    /// <summary>
    /// 评论访问控制设置
    /// </summary>
    [JsonPropertyName("comment_access_control")]
    public int CommentAccessControl { get; init; }
}

/// <summary>
/// 小说系列详情响应
/// </summary>
public record NovelSeriesDetail : PaginatedResponse
{
    /// <summary>
    /// 小说系列详情信息
    /// </summary>
    [JsonPropertyName("novel_series_detail")]
    public NovelSeriesDetailInfo? NovelSeriesDetailInfo { get; init; }

    /// <summary>
    /// 系列首篇小说
    /// </summary>
    [JsonPropertyName("novel_series_first_novel")]
    public NovelInfo? NovelSeriesFirstNovel { get; init; }

    /// <summary>
    /// 系列小说列表
    /// </summary>
    [JsonPropertyName("novels")]
    public List<NovelInfo>? Novels { get; init; }
}

/// <summary>
/// 小说系列详情信息
/// </summary>
public record NovelSeriesDetailInfo
{
    /// <summary>
    /// 系列ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 系列标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// 系列描述
    /// </summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; init; }

    /// <summary>
    /// 是否原创作品
    /// </summary>
    [JsonPropertyName("is_original")]
    public bool IsOriginal { get; init; }

    /// <summary>
    /// 是否已完结
    /// </summary>
    [JsonPropertyName("is_concluded")]
    public bool IsConcluded { get; init; }

    /// <summary>
    /// 内容数量
    /// </summary>
    [JsonPropertyName("content_count")]
    public int ContentCount { get; init; }

    /// <summary>
    /// 总字符数
    /// </summary>
    [JsonPropertyName("total_character_count")]
    public int TotalCharacterCount { get; init; }

    /// <summary>
    /// 系列作者信息
    /// </summary>
    [JsonPropertyName("user")]
    public UserInfo? User { get; init; }

    /// <summary>
    /// 显示文本
    /// </summary>
    [JsonPropertyName("display_text")]
    public string? DisplayText { get; init; }

    /// <summary>
    /// 是否已添加到关注列表
    /// </summary>
    [JsonPropertyName("watchlist_added")]
    public bool WatchlistAdded { get; init; }
}

/// <summary>
/// 网页小说内容响应
/// </summary>
public record WebviewNovel : BaseResponse
{
    /// <summary>
    /// 小说正文内容
    /// </summary>
    [JsonPropertyName("novel_text")]
    public string? NovelText { get; set; } // 设置为可变，用于动态赋值

    /// <summary>
    /// 原始文本内容
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; } // HTML中提取的原始text字段

    /// <summary>
    /// 小说标记信息
    /// </summary>
    [JsonPropertyName("novel_marker")]
    public NovelMarker? NovelMarker { get; init; }

    /// <summary>
    /// 系列导航信息
    /// </summary>
    [JsonPropertyName("series_navigation")]
    public SeriesNavigation? SeriesNavigation { get; init; }

    /// <summary>
    /// 小说ID
    /// </summary>
    // 支持其他可能的字段
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// 小说标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// 小说作者信息
    /// </summary>
    [JsonPropertyName("user")]
    public UserInfo? User { get; init; }
}


/// <summary>
/// 小说标记信息
/// </summary>
public record NovelMarker
{
    /// <summary>
    /// 页面编号
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; init; }
}

/// <summary>
/// 系列导航信息
/// </summary>
public record SeriesNavigation
{
    /// <summary>
    /// 上一篇小说信息
    /// </summary>
    [JsonPropertyName("prev_novel")]
    public NovelNavigationInfo? PrevNovel { get; init; }

    /// <summary>
    /// 下一篇小说信息
    /// </summary>
    [JsonPropertyName("next_novel")]
    public NovelNavigationInfo? NextNovel { get; init; }
}

/// <summary>
/// 小说导航信息
/// </summary>
public record NovelNavigationInfo
{
    /// <summary>
    /// 小说ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 小说标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

/// <summary>
/// 评论信息（小说专用）
/// </summary>
public record Comment
{
    /// <summary>
    /// 评论ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [JsonPropertyName("comment")]
    public string? CommentText { get; init; }

    /// <summary>
    /// 评论时间
    /// </summary>
    [JsonPropertyName("date")]
    [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
    public required DateTimeOffset Date { get; init; }

    /// <summary>
    /// 评论用户信息
    /// </summary>
    [JsonPropertyName("user")]
    public UserInfo? User { get; init; }

    /// <summary>
    /// 父评论信息
    /// </summary>
    [JsonPropertyName("parent_comment")]
    public ParentComment? ParentComment { get; init; }
}

/// <summary>
/// 父评论信息
/// </summary>
public record ParentComment
{
    /// <summary>
    /// 父评论ID
    /// </summary>
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    /// <summary>
    /// 父评论内容
    /// </summary>
    [JsonPropertyName("comment")]
    public string? CommentText { get; init; }

    /// <summary>
    /// 父评论时间
    /// </summary>
    [JsonPropertyName("date")]
    [JsonConverter(typeof(NullableDateTimeOffsetJsonConverter))]
    public DateTimeOffset? Date { get; init; }

    /// <summary>
    /// 父评论用户信息
    /// </summary>
    [JsonPropertyName("user")]
    public UserInfo? User { get; init; }
}