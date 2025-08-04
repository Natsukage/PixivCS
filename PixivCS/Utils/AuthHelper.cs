using System.Security.Cryptography;
using System.Text;

namespace PixivCS.Utils;

/// <summary>
/// Pixiv 认证辅助工具类
/// </summary>
public static class AuthHelper
{
    /// <summary>
    /// Pixiv 的 Hash Secret
    /// </summary>
    private const string HashSecret = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c";
    
    /// <summary>
    /// 生成带有时间戳和哈希签名的认证请求头
    /// </summary>
    public static Dictionary<string, string> CreateAuthHeaders()
    {
        var clientTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");
        var clientHash = CalculateMD5Hash(clientTime + HashSecret);
        
        return new Dictionary<string, string>
        {
            ["User-Agent"] = "PixivIOSApp/7.13.3 (iOS 14.6; iPhone13,2)",
            ["App-OS"] = "ios",
            ["App-OS-Version"] = "14.6",
            ["X-Client-Time"] = clientTime,
            ["X-Client-Hash"] = clientHash,
            ["Content-Type"] = "application/x-www-form-urlencoded"
        };
    }
    
    /// <summary>
    /// 计算字符串的 MD5 哈希值
    /// </summary>
    private static string CalculateMD5Hash(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 将字典转换为 URL 编码的表单数据
    /// </summary>
    public static StringContent CreateFormContent(Dictionary<string, string> data)
    {
        var formData = string.Join("&", data.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        
        return new StringContent(formData, Encoding.UTF8, "application/x-www-form-urlencoded");
    }
}