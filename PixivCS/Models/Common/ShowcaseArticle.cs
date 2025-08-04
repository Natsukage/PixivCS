namespace PixivCS.Models.Common;

/// <summary>
/// 特辑文章响应
/// </summary>
public record ShowcaseArticle : BaseResponse
{
    /// <summary>
    /// 特辑文章内容列表
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("body")]
    public List<ShowcaseBody>? Body { get; init; }
}

/// <summary>
/// 特辑文章内容
/// </summary>
public record ShowcaseBody
{
    /// <summary>
    /// 特辑内容ID
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// 语言代码
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("lang")]
    public string? Lang { get; init; }

    /// <summary>
    /// 特辑条目内容
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("entry")]
    public ShowcaseEntry? Entry { get; init; }
}

/// <summary>
/// 特辑文章条目
/// </summary>
public record ShowcaseEntry
{
    /// <summary>
    /// 特辑条目ID
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// 文章标题
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// 纯文本标题（去除HTML标签）
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("pure_title")]
    public string? PureTitle { get; init; }

    /// <summary>
    /// 广告语或副标题
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("catchphrase")]
    public string? Catchphrase { get; init; }

    /// <summary>
    /// 文章头部HTML内容
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("header")]
    public string? Header { get; init; }

    /// <summary>
    /// 文章主体HTML内容
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("body")]
    public string? Body { get; init; }

    /// <summary>
    /// 文章底部HTML内容
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("footer")]
    public string? Footer { get; init; }

    /// <summary>
    /// 侧边栏HTML内容
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("sidebar")]
    public string? Sidebar { get; init; }

    /// <summary>
    /// 文章发布时间
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("publish_date")]
    public DateTime PublishDate { get; init; }

    /// <summary>
    /// 文章语言
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("language")]
    public string? Language { get; init; }

    /// <summary>
    /// Pixiv用户ID
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("pixiv_user_id")]
    public int PixivUserId { get; init; }

    /// <summary>
    /// Twitter用户名
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("twitter_username")]
    public string? TwitterUsername { get; init; }

    /// <summary>
    /// 外部用户ID
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("external_user_id")]
    public string? ExternalUserId { get; init; }

    /// <summary>
    /// 作者名称
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("author_name")]
    public string? AuthorName { get; init; }

    /// <summary>
    /// 作者头像图URL
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("profile_img")]
    public string? ProfileImg { get; init; }

    /// <summary>
    /// 主要标签
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tag")]
    public string? Tag { get; init; }

    /// <summary>
    /// 所有标签列表
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    /// <summary>
    /// 文章分类
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("category")]
    public string? Category { get; init; }

    /// <summary>
    /// 文章子分类
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("subcategory")]
    public string? Subcategory { get; init; }
}