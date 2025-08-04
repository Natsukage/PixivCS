using System.Text.Json;
using System.Text.Json.Serialization;

namespace PixivCS.Utils;

/// <summary>
/// DateTimeOffset 的 JSON 转换器，处理 Pixiv API 的日期时间格式
/// </summary>
public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    /// <summary>
    /// 从 JSON 读取 DateTimeOffset 值
    /// </summary>
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">要转换的类型</param>
    /// <param name="options">JSON 序列化选项</param>
    /// <returns>转换后的 DateTimeOffset 值</returns>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            return DateTimeOffset.MinValue;
        }

        if (DateTimeOffset.TryParse(dateString, out var result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse date string: {dateString}");
    }

    /// <summary>
    /// 将 DateTimeOffset 值写入 JSON
    /// </summary>
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">要序列化的 DateTimeOffset 值</param>
    /// <param name="options">JSON 序列化选项</param>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:sszzz"));
    }
}

/// <summary>
/// 可空 DateTimeOffset 的 JSON 转换器
/// </summary>
public class NullableDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
{
    /// <summary>
    /// 从 JSON 读取可空 DateTimeOffset 值
    /// </summary>
    /// <param name="reader">JSON 读取器</param>
    /// <param name="typeToConvert">要转换的类型</param>
    /// <param name="options">JSON 序列化选项</param>
    /// <returns>转换后的可空 DateTimeOffset 值</returns>
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(dateString, out var result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse date string: {dateString}");
    }

    /// <summary>
    /// 将可空 DateTimeOffset 值写入 JSON
    /// </summary>
    /// <param name="writer">JSON 写入器</param>
    /// <param name="value">要序列化的可空 DateTimeOffset 值</param>
    /// <param name="options">JSON 序列化选项</param>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}