namespace PixivCS.Models.Common;

/// <summary>
/// 插画类型
/// </summary>
public enum IllustType
{
    /// <summary>
    /// 插画
    /// </summary>
    Illust,
    /// <summary>
    /// 漫画
    /// </summary>
    Manga
}

/// <summary>
/// 内容类型
/// </summary>
public enum ContentType
{
    /// <summary>
    /// 插画
    /// </summary>
    Illust,
    /// <summary>
    /// 漫画
    /// </summary>
    Manga
}

/// <summary>
/// 限制类型
/// </summary>
public enum RestrictType
{
    /// <summary>
    /// 公开
    /// </summary>
    Public,
    /// <summary>
    /// 私人
    /// </summary>
    Private
}

/// <summary>
/// 排行榜模式
/// </summary>
public enum RankingMode
{
    /// <summary>
    /// 日榜
    /// </summary>
    Day,
    /// <summary>
    /// 周榜
    /// </summary>
    Week,
    /// <summary>
    /// 月榜
    /// </summary>
    Month,
    /// <summary>
    /// 男性向日榜
    /// </summary>
    DayMale,
    /// <summary>
    /// 女性向日榜
    /// </summary>
    DayFemale,
    /// <summary>
    /// 原创周榜
    /// </summary>
    WeekOriginal,
    /// <summary>
    /// 新人周榜
    /// </summary>
    WeekRookie,
    /// <summary>
    /// 漫画日榜
    /// </summary>
    DayManga,
    /// <summary>
    /// R18日榜
    /// </summary>
    DayR18,
    /// <summary>
    /// R18男性向日榜
    /// </summary>
    DayMaleR18,
    /// <summary>
    /// R18女性向日榜
    /// </summary>
    DayFemaleR18,
    /// <summary>
    /// R18周榜
    /// </summary>
    WeekR18,
    /// <summary>
    /// R18G周榜
    /// </summary>
    WeekR18G
}

/// <summary>
/// 搜索目标
/// </summary>
public enum SearchTarget
{
    /// <summary>
    /// 标签部分匹配
    /// </summary>
    PartialMatchForTags,
    /// <summary>
    /// 标签完全匹配
    /// </summary>
    ExactMatchForTags,
    /// <summary>
    /// 标题和描述
    /// </summary>
    TitleAndCaption,
    /// <summary>
    /// 关键词
    /// </summary>
    Keyword
}

/// <summary>
/// 排序顺序
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// 日期降序（新到旧）
    /// </summary>
    DateDesc,
    /// <summary>
    /// 日期升序（旧到新）
    /// </summary>
    DateAsc,
    /// <summary>
    /// 人气降序（高到低）
    /// </summary>
    PopularDesc
}

/// <summary>
/// 时间范围
/// </summary>
public enum Duration
{
    /// <summary>
    /// 过去24小时内
    /// </summary>
    WithinLastDay,
    /// <summary>
    /// 过去一周内
    /// </summary>
    WithinLastWeek,
    /// <summary>
    /// 过去一个月内
    /// </summary>
    WithinLastMonth
}

/// <summary>
/// 连接模式
/// </summary>
public enum ConnectionMode
{
    /// <summary>
    /// 正常连接
    /// </summary>
    Normal,
    
    /// <summary>
    /// 使用代理
    /// </summary>
    Proxy,
    
    /// <summary>
    /// 免代理直连
    /// </summary>
    DirectBypass
}

