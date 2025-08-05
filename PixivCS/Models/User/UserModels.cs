using System.Text.Json.Serialization;
using PixivCS.Models.Common;

namespace PixivCS.Models.User;

/// <summary>
/// 用户基础信息
/// </summary>
public record UserInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// 用户账号
    /// </summary>
    [JsonPropertyName("account")]
    public required string Account { get; init; }

    /// <summary>
    /// 用户头像URL信息
    /// </summary>
    [JsonPropertyName("profile_image_urls")]
    public required ProfileImageUrls ProfileImageUrls { get; init; }

    /// <summary>
    /// 用户简介
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; } // present only on user_detail endpoint

    /// <summary>
    /// 是否已关注该用户
    /// </summary>
    [JsonPropertyName("is_followed")]
    public bool? IsFollowed { get; init; }

    /// <summary>
    /// 是否被该用户屏蔽访问
    /// </summary>
    [JsonPropertyName("is_access_blocking_user")]
    public bool? IsAccessBlockingUser { get; init; }

    /// <summary>
    /// 是否接受关注请求
    /// </summary>
    [JsonPropertyName("is_accept_request")]
    public bool? IsAcceptRequest { get; init; } // present on user_detail (v2), user_following and user_follower endpoints
}

/// <summary>
/// 用户详细信息
/// </summary>
public record UserDetail : BaseResponse
{
    /// <summary>
    /// 用户详细信息
    /// </summary>
    [JsonPropertyName("user")]
    public UserDetailInfo? User { get; init; }

    /// <summary>
    /// 用户个人资料
    /// </summary>
    [JsonPropertyName("profile")]
    public UserProfile? Profile { get; init; }

    /// <summary>
    /// 个人资料公开设置
    /// </summary>
    [JsonPropertyName("profile_publicity")]
    public UserProfilePublicity? ProfilePublicity { get; init; }

    /// <summary>
    /// 工作空间信息
    /// </summary>
    [JsonPropertyName("workspace")]
    public UserWorkspace? Workspace { get; init; }

    /// <summary>
    /// 禁用的链接列表
    /// </summary>
    [JsonPropertyName("disabled_links")]
    public List<string>? DisabledLinks { get; init; }
}

/// <summary>
/// 用户详细信息（继承自 UserInfo，目前两者实际相同，只是为了预防未来变动所以没有合并）
/// </summary>
public record UserDetailInfo : UserInfo
{
    // UserInfo 已经包含了 comment 字段，不需要重复定义
}

/// <summary>
/// 用户档案信息
/// </summary>
public record UserProfile
{
    /// <summary>
    /// 用户网页链接
    /// </summary>
    [JsonPropertyName("webpage")]
    public string? Webpage { get; init; }

    /// <summary>
    /// 性别 (0=未设置, 1=男性, 2=女性)
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; init; }

    /// <summary>
    /// 生日信息
    /// </summary>
    [JsonPropertyName("birth")]
    public string? Birth { get; init; }

    /// <summary>
    /// 生日日期
    /// </summary>
    [JsonPropertyName("birth_day")]
    public string? BirthDay { get; init; }

    /// <summary>
    /// 出生年份
    /// </summary>
    [JsonPropertyName("birth_year")]
    public int? BirthYear { get; init; }

    /// <summary>
    /// 地区
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    /// <summary>
    /// 地址ID
    /// </summary>
    [JsonPropertyName("address_id")]
    public int? AddressId { get; init; }

    /// <summary>
    /// 国家代码
    /// </summary>
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; init; }

    /// <summary>
    /// 职业
    /// </summary>
    [JsonPropertyName("job")]
    public string? Job { get; init; }

    /// <summary>
    /// 职业ID
    /// </summary>
    [JsonPropertyName("job_id")]
    public int? JobId { get; init; }

    /// <summary>
    /// 关注用户总数
    /// </summary>
    [JsonPropertyName("total_follow_users")]
    public int TotalFollowUsers { get; init; }

    /// <summary>
    /// MyPixiv好友总数
    /// </summary>
    [JsonPropertyName("total_mypixiv_users")]
    public int TotalMypixivUsers { get; init; }

    /// <summary>
    /// 插画作品总数
    /// </summary>
    [JsonPropertyName("total_illusts")]
    public int TotalIllusts { get; init; }

    /// <summary>
    /// 漫画作品总数
    /// </summary>
    [JsonPropertyName("total_manga")]
    public int TotalManga { get; init; }

    /// <summary>
    /// 小说作品总数
    /// </summary>
    [JsonPropertyName("total_novels")]
    public int TotalNovels { get; init; }

    /// <summary>
    /// 公开插画收藏总数（仅在v1端点可用）
    /// </summary>
    [JsonPropertyName("total_illust_bookmarks_public")]
    public int? TotalIllustBookmarksPublic { get; init; } // only available in v1 endpoint

    /// <summary>
    /// 插画系列总数
    /// </summary>
    [JsonPropertyName("total_illust_series")]
    public int TotalIllustSeries { get; init; }

    /// <summary>
    /// 小说系列总数
    /// </summary>
    [JsonPropertyName("total_novel_series")]
    public int TotalNovelSeries { get; init; }

    /// <summary>
    /// 背景图片URL
    /// </summary>
    [JsonPropertyName("background_image_url")]
    public string? BackgroundImageUrl { get; init; }

    /// <summary>
    /// Twitter账号
    /// </summary>
    [JsonPropertyName("twitter_account")]
    public string? TwitterAccount { get; init; }

    /// <summary>
    /// Twitter链接
    /// </summary>
    [JsonPropertyName("twitter_url")]
    public string? TwitterUrl { get; init; }

    /// <summary>
    /// 是否为高级会员
    /// </summary>
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; init; }

    /// <summary>
    /// 是否使用自定义头像
    /// </summary>
    [JsonPropertyName("is_using_custom_profile_image")]
    public bool IsUsingCustomProfileImage { get; init; }
}

/// <summary>
/// 用户隐私设置
/// </summary>
public record UserProfilePublicity
{
    /// <summary>
    /// 性别公开设置
    /// </summary>
    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    /// <summary>
    /// 地区公开设置
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    /// <summary>
    /// 生日公开设置
    /// </summary>
    [JsonPropertyName("birth_day")]
    public string? BirthDay { get; init; }

    /// <summary>
    /// 出生年份公开设置
    /// </summary>
    [JsonPropertyName("birth_year")]
    public string? BirthYear { get; init; }

    /// <summary>
    /// 职业公开设置
    /// </summary>
    [JsonPropertyName("job")]
    public string? Job { get; init; }
}

/// <summary>
/// 用户工作环境
/// </summary>
public record UserWorkspace
{
    /// <summary>
    /// 电脑配置
    /// </summary>
    [JsonPropertyName("pc")]
    public string? Pc { get; init; }

    /// <summary>
    /// 显示器
    /// </summary>
    [JsonPropertyName("monitor")]
    public string? Monitor { get; init; }

    /// <summary>
    /// 绘图工具
    /// </summary>
    [JsonPropertyName("tool")]
    public string? Tool { get; init; }

    /// <summary>
    /// 扫描仪
    /// </summary>
    [JsonPropertyName("scanner")]
    public string? Scanner { get; init; }

    /// <summary>
    /// 数位板
    /// </summary>
    [JsonPropertyName("tablet")]
    public string? Tablet { get; init; }

    /// <summary>
    /// 鼠标
    /// </summary>
    [JsonPropertyName("mouse")]
    public string? Mouse { get; init; }

    /// <summary>
    /// 打印机
    /// </summary>
    [JsonPropertyName("printer")]
    public string? Printer { get; init; }

    /// <summary>
    /// 桌面环境
    /// </summary>
    [JsonPropertyName("desktop")]
    public string? Desktop { get; init; }

    /// <summary>
    /// 音乐
    /// </summary>
    [JsonPropertyName("music")]
    public string? Music { get; init; }

    /// <summary>
    /// 桌子
    /// </summary>
    [JsonPropertyName("desk")]
    public string? Desk { get; init; }

    /// <summary>
    /// 椅子
    /// </summary>
    [JsonPropertyName("chair")]
    public string? Chair { get; init; }

    /// <summary>
    /// 工作环境备注
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}

/// <summary>
/// 用户关注列表响应
/// </summary>
public record UserFollowList : PaginatedResponse
{
    /// <summary>
    /// 用户预览列表
    /// </summary>
    [JsonPropertyName("user_previews")]
    public List<UserPreview>? UserPreviews { get; init; }
}

/// <summary>
/// 用户预览信息
/// </summary>
public record UserPreview
{
    /// <summary>
    /// 用户信息
    /// </summary>
    [JsonPropertyName("user")]
    public UserInfo? User { get; init; }

    /// <summary>
    /// 插画预览列表
    /// </summary>
    [JsonPropertyName("illusts")]
    public List<IllustPreview>? Illusts { get; init; }

    /// <summary>
    /// 小说预览列表
    /// </summary>
    [JsonPropertyName("novels")]
    public List<NovelPreview>? Novels { get; init; }

    /// <summary>
    /// 是否已屏蔽
    /// </summary>
    [JsonPropertyName("is_muted")]
    public bool IsMuted { get; init; }
}

/// <summary>
/// 插画预览信息
/// </summary>
public record IllustPreview
{
    /// <summary>
    /// 插画ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// 插画标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// 插画类型
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// 图片URL信息
    /// </summary>
    [JsonPropertyName("image_urls")]
    public ImageUrls? ImageUrls { get; init; }

    /// <summary>
    /// 是否已收藏
    /// </summary>
    [JsonPropertyName("is_bookmarked")]
    public bool IsBookmarked { get; init; }

    /// <summary>
    /// 是否已屏蔽
    /// </summary>
    [JsonPropertyName("is_muted")]
    public bool IsMuted { get; init; }
}

/// <summary>
/// 小说预览信息
/// </summary>
public record NovelPreview
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

    /// <summary>
    /// 小说简介
    /// </summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; init; }

    /// <summary>
    /// 是否已收藏
    /// </summary>
    [JsonPropertyName("is_bookmarked")]
    public bool IsBookmarked { get; init; }

    /// <summary>
    /// 是否已屏蔽
    /// </summary>
    [JsonPropertyName("is_muted")]
    public bool IsMuted { get; init; }
}

/// <summary>
/// 评论用户信息（与 UserInfo 略有不同，不包含某些字段）
/// </summary>
public record CommentUser
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// 用户账号
    /// </summary>
    [JsonPropertyName("account")]
    public required string Account { get; init; }

    /// <summary>
    /// 用户头像URL信息
    /// </summary>
    [JsonPropertyName("profile_image_urls")]
    public required ProfileImageUrls ProfileImageUrls { get; init; }
}