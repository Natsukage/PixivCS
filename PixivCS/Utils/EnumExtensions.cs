using PixivCS.Models.Common;

namespace PixivCS.Utils;

/// <summary>
/// 枚举扩展方法
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 将 IllustType 转换为 API 参数字符串
    /// </summary>
    public static string ToApiString(this IllustType type) => type switch
    {
        IllustType.Illust => "illust",
        IllustType.Manga => "manga",
        _ => "illust"
    };

    /// <summary>
    /// 将 ContentType 转换为 API 参数字符串
    /// </summary>
    public static string ToApiString(this ContentType type) => type switch
    {
        ContentType.Illust => "illust",
        ContentType.Manga => "manga",
        _ => "illust"
    };

    /// <summary>
    /// 将 RestrictType 转换为 API 参数字符串
    /// </summary>
    public static string ToApiString(this RestrictType type) => type switch
    {
        RestrictType.Public => "public",
        RestrictType.Private => "private",
        _ => "public"
    };

    /// <summary>
    /// 将 RankingMode 转换为 API 参数字符串
    /// </summary>
    public static string ToApiString(this RankingMode mode) => mode switch
    {
        RankingMode.Day => "day",
        RankingMode.Week => "week",
        RankingMode.Month => "month",
        RankingMode.DayMale => "day_male",
        RankingMode.DayFemale => "day_female",
        RankingMode.WeekOriginal => "week_original",
        RankingMode.WeekRookie => "week_rookie",
        RankingMode.DayManga => "day_manga",
        RankingMode.DayR18 => "day_r18",
        RankingMode.DayMaleR18 => "day_male_r18",
        RankingMode.DayFemaleR18 => "day_female_r18",
        RankingMode.WeekR18 => "week_r18",
        RankingMode.WeekR18G => "week_r18g",
        _ => "day"
    };

    /// <summary>
    /// 将 SearchTarget 转换为 API 参数字符串
    /// </summary>
    public static string ToApiString(this SearchTarget target) => target switch
    {
        SearchTarget.PartialMatchForTags => "partial_match_for_tags",
        SearchTarget.ExactMatchForTags => "exact_match_for_tags",
        SearchTarget.TitleAndCaption => "title_and_caption",
        SearchTarget.Keyword => "keyword",
        _ => "partial_match_for_tags"
    };

    /// <summary>
    /// 将 SortOrder 转换为 API 参数字符串
    /// </summary>
    public static string ToApiString(this SortOrder order) => order switch
    {
        SortOrder.DateDesc => "date_desc",
        SortOrder.DateAsc => "date_asc",
        SortOrder.PopularDesc => "popular_desc",
        _ => "date_desc"
    };

    /// <summary>
    /// 将 Duration 转换为 API 参数字符串
    /// </summary>
    public static string? ToApiString(this Duration? duration) => duration switch
    {
        Duration.WithinLastDay => "within_last_day",
        Duration.WithinLastWeek => "within_last_week",
        Duration.WithinLastMonth => "within_last_month",
        _ => null
    };

    /// <summary>
    /// 将布尔值转换为 API 参数字符串
    /// </summary>
    public static string ToApiString(this bool value) => value ? "true" : "false";
}