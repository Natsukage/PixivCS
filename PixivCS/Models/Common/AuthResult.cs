namespace PixivCS.Models.Common;

/// <summary>
/// 认证结果
/// </summary>
public record AuthResult : BaseResponse
{
    /// <summary>
    /// 访问令牌，用于API请求的身份验证
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }
    
    /// <summary>
    /// 刷新令牌，用于获取新的访问令牌
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
    
    /// <summary>
    /// 令牌有效期（秒）
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    
    /// <summary>
    /// 认证用户信息
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("user")]
    public AuthUser? User { get; init; }
    
    /// <summary>
    /// 用户ID（从User.Id获取）
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId => User?.Id;
    
    /// <summary>
    /// 令牌获取时间
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime IssuedAt { get; init; } = DateTime.Now;
    
    /// <summary>
    /// 令牌过期时间
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime ExpiresAt => IssuedAt.AddSeconds(ExpiresIn);
    
    /// <summary>
    /// 检查令牌是否需要刷新（即将过期或已过期）
    /// 提前5分钟开始需要刷新
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool ShouldRefresh => DateTime.Now >= ExpiresAt.AddMinutes(-5);
    
    /// <summary>
    /// 检查令牌是否已过期
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsExpired => DateTime.Now >= ExpiresAt;
}

/// <summary>
/// 认证用户信息
/// </summary>
public record AuthUser
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string? Id { get; init; }
    
    /// <summary>
    /// 用户昵称
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; init; }
    
    /// <summary>
    /// 用户账号名
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("account")]
    public string? Account { get; init; }
    
    /// <summary>
    /// 用户邮箱地址
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("mail_address")]
    public string? MailAddress { get; init; }
    
    /// <summary>
    /// 是否为会员用户，true表示会员，false表示普通用户
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("is_premium")]
    public bool IsPremium { get; init; }
}