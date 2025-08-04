using System.Text.Json.Serialization;
using PixivCS.Models.Common;
using PixivCS.Models.User;
using PixivCS.Utils;

namespace PixivCS.Models.Illust;

/// <summary>
/// 插画基础信息
/// </summary>
public record IllustInfo
{
    /// <summary>
    /// 插画ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// 插画标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    /// <summary>
    /// 插画类型（illust、manga等）
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    /// <summary>
    /// 插画图片URL信息
    /// </summary>
    [JsonPropertyName("image_urls")]
    public ImageUrls? ImageUrls { get; init; }

    /// <summary>
    /// 插画描述文字
    /// </summary>
    [JsonPropertyName("caption")]
    public string Caption { get; init; } = "";

    /// <summary>
    /// 限制级别
    /// </summary>
    [JsonPropertyName("restrict")]
    public int Restrict { get; init; }

    /// <summary>
    /// 插画作者信息
    /// </summary>
    [JsonPropertyName("user")]
    public UserInfo? User { get; init; }

    /// <summary>
    /// 插画标签列表
    /// </summary>
    [JsonPropertyName("tags")]
    public List<IllustTag> Tags { get; init; } = [];

    /// <summary>
    /// 创作工具列表
    /// </summary>
    [JsonPropertyName("tools")]
    public List<string> Tools { get; init; } = [];

    /// <summary>
    /// 插画创建时间
    /// </summary>
    [JsonPropertyName("create_date")]
    [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
    public DateTimeOffset CreateDate { get; init; }

    /// <summary>
    /// 页面数量（多页插画的页数）
    /// </summary>
    [JsonPropertyName("page_count")]
    public int PageCount { get; init; }

    /// <summary>
    /// 插画宽度（像素）
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; init; }

    /// <summary>
    /// 插画高度（像素）
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; init; }

    /// <summary>
    /// 安全等级
    /// </summary>
    [JsonPropertyName("sanity_level")]
    public int SanityLevel { get; init; }

    /// <summary>
    /// X级限制标识
    /// </summary>
    [JsonPropertyName("x_restrict")]
    public int XRestrict { get; init; }

    /// <summary>
    /// 插画系列信息
    /// </summary>
    [JsonPropertyName("series")]
    public IllustSeries? Series { get; init; } // 可能为 null

    /// <summary>
    /// 单页图片元数据
    /// </summary>
    [JsonPropertyName("meta_single_page")]
    public MetaSinglePage? MetaSinglePage { get; init; }

    /// <summary>
    /// 多页图片元数据列表
    /// </summary>
    [JsonPropertyName("meta_pages")]
    public List<MetaPage> MetaPages { get; init; } = [];

    /// <summary>
    /// 总浏览数
    /// </summary>
    [JsonPropertyName("total_view")]
    public int TotalView { get; init; }

    /// <summary>
    /// 总收藏数
    /// </summary>
    [JsonPropertyName("total_bookmarks")]
    public int TotalBookmarks { get; init; }

    /// <summary>
    /// 是否已收藏
    /// </summary>
    [JsonPropertyName("is_bookmarked")]
    public bool IsBookmarked { get; init; }

    /// <summary>
    /// 是否可见
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; init; }

    /// <summary>
    /// 是否被屏蔽
    /// </summary>
    [JsonPropertyName("is_muted")]
    public bool IsMuted { get; init; }

    /// <summary>
    /// AI插画类型标识
    /// </summary>
    [JsonPropertyName("illust_ai_type")]
    public int IllustAiType { get; init; }

    /// <summary>
    /// 插画书籍样式
    /// </summary>
    [JsonPropertyName("illust_book_style")]
    public int IllustBookStyle { get; init; }

    /// <summary>
    /// 总评论数
    /// </summary>
    [JsonPropertyName("total_comments")]
    public int? TotalComments { get; init; } // 可能为 null

    /// <summary>
    /// 限制属性列表
    /// </summary>
    [JsonPropertyName("restriction_attributes")]
    public List<string> RestrictionAttributes { get; init; } = [];
}

/// <summary>
/// 插画标签（不包含 added_by_uploaded_user 字段）
/// </summary>
public record IllustTag : Tag
{
    // 插画标签只有 name 和 translated_name，没有 added_by_uploaded_user
}

/// <summary>
/// 插画系列信息
/// </summary>
public record IllustSeries
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
/// 单页图片元数据
/// </summary>
public record MetaSinglePage
{
    /// <summary>
    /// 原图URL
    /// </summary>
    [JsonPropertyName("original_image_url")]
    public string? OriginalImageUrl { get; init; }
}

/// <summary>
/// 多页图片元数据
/// </summary>
public record MetaPage
{
    /// <summary>
    /// 图片URL信息
    /// </summary>
    [JsonPropertyName("image_urls")]
    public ImageUrls? ImageUrls { get; init; }
}

/// <summary>
/// 插画详情响应
/// </summary>
public record IllustDetail : BaseResponse
{
    /// <summary>
    /// 插画信息
    /// </summary>
    [JsonPropertyName("illust")]
    public IllustInfo? Illust { get; init; }
}

/// <summary>
/// 插画列表响应
/// </summary>
public record IllustList : PaginatedResponse
{
    /// <summary>
    /// 插画列表
    /// </summary>
    [JsonPropertyName("illusts")]
    public List<IllustInfo>? Illusts { get; init; }
}

/// <summary>
/// 用户插画列表响应
/// </summary>
public record UserIllusts : IllustList;

/// <summary>
/// 推荐插画响应
/// </summary>
public record IllustRecommended : PaginatedResponse
{
    /// <summary>
    /// 推荐插画列表
    /// </summary>
    [JsonPropertyName("illusts")]
    public List<IllustInfo>? Illusts { get; init; }

    /// <summary>
    /// 排行榜插画列表
    /// </summary>
    [JsonPropertyName("ranking_illusts")]
    public List<IllustInfo>? RankingIllusts { get; init; }

    /// <summary>
    /// 是否存在竞赛
    /// </summary>
    [JsonPropertyName("contest_exists")]
    public bool ContestExists { get; init; }

    /// <summary>
    /// 隐私政策信息
    /// </summary>
    [JsonPropertyName("privacy_policy")]
    public PrivacyPolicy? PrivacyPolicy { get; init; }
}

/// <summary>
/// 隐私政策信息
/// </summary>
public record PrivacyPolicy
{
    /// <summary>
    /// 隐私政策版本
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>
    /// 隐私政策消息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// 隐私政策URL
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

/// <summary>
/// 插画评论响应
/// </summary>
public record IllustComments : PaginatedResponse
{
    /// <summary>
    /// 评论列表
    /// </summary>
    [JsonPropertyName("comments")]
    public List<Comment>? Comments { get; init; }

    /// <summary>
    /// 总评论数
    /// </summary>
    [JsonPropertyName("total_comments")]
    public int TotalComments { get; init; }
}

/// <summary>
/// 评论信息
/// </summary>
public record Comment
{
    /// <summary>
    /// 评论ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [JsonPropertyName("comment")]
    public required string CommentText { get; init; }

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
    public CommentUser? User { get; init; } // 可能为 null

    /// <summary>
    /// 父评论（回复）
    /// </summary>
    [JsonPropertyName("parent_comment")]
    public Comment? ParentComment { get; init; } // 可能为 null，递归引用
}


/// <summary>
/// 插画收藏详情响应
/// </summary>
public record IllustBookmarkDetail : BaseResponse
{
    /// <summary>
    /// 收藏详情信息
    /// </summary>
    [JsonPropertyName("bookmark_detail")]
    public BookmarkDetail? BookmarkDetail { get; init; }
}

/// <summary>
/// 收藏详情
/// </summary>
public record BookmarkDetail
{
    /// <summary>
    /// 是否已收藏
    /// </summary>
    [JsonPropertyName("is_bookmarked")]
    public bool IsBookmarked { get; init; }

    /// <summary>
    /// 收藏标签列表
    /// </summary>
    [JsonPropertyName("tags")]
    public List<BookmarkTag>? Tags { get; init; }

    /// <summary>
    /// 收藏限制设置
    /// </summary>
    [JsonPropertyName("restrict")]
    public string? Restrict { get; init; }
}

/// <summary>
/// 收藏标签
/// </summary>
public record BookmarkTag
{
    /// <summary>
    /// 标签名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// 是否已注册
    /// </summary>
    [JsonPropertyName("is_registered")]
    public bool IsRegistered { get; init; }
}

/// <summary>
/// 热门标签响应
/// </summary>
public record TrendingTagsIllust : BaseResponse
{
    /// <summary>
    /// 热门标签列表
    /// </summary>
    [JsonPropertyName("trend_tags")]
    public List<TrendingTag>? TrendTags { get; init; }
}

/// <summary>
/// 热门标签
/// </summary>
public record TrendingTag
{
    /// <summary>
    /// 标签名称
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; init; }

    /// <summary>
    /// 标签翻译名称
    /// </summary>
    [JsonPropertyName("translated_name")]
    public string? TranslatedName { get; init; }

    /// <summary>
    /// 代表插画
    /// </summary>
    [JsonPropertyName("illust")]
    public IllustInfo? Illust { get; init; }
}

/// <summary>
/// 动图元数据响应
/// </summary>
public record UgoiraMetadata : BaseResponse
{
    /// <summary>
    /// 动图元数据信息
    /// </summary>
    [JsonPropertyName("ugoira_metadata")]
    public UgoiraMetadataInfo? UgoiraMetadataInfo { get; init; }
}

/// <summary>
/// 动图元数据信息
/// </summary>
public record UgoiraMetadataInfo
{
    /// <summary>
    /// ZIP文件URL信息
    /// </summary>
    [JsonPropertyName("zip_urls")]
    public ZipUrls? ZipUrls { get; init; }

    /// <summary>
    /// 动图帧列表
    /// </summary>
    [JsonPropertyName("frames")]
    public List<UgoiraFrame>? Frames { get; init; }
}

/// <summary>
/// 动图 ZIP 文件 URL
/// </summary>
public record ZipUrls
{
    /// <summary>
    /// 中等质量ZIP文件URL
    /// </summary>
    [JsonPropertyName("medium")]
    public string? Medium { get; init; }
}

/// <summary>
/// 动图帧信息
/// </summary>
public record UgoiraFrame
{
    /// <summary>
    /// 帧文件名
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; init; }

    /// <summary>
    /// 帧持续时间（毫秒）
    /// </summary>
    [JsonPropertyName("delay")]
    public int Delay { get; init; }
}