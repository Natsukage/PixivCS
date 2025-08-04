using PixivCS.Exceptions;
using PixivCS.Models.Common;
using PixivCS.Models.Illust;
using PixivCS.Models.Novel;
using PixivCS.Models.Search;
using PixivCS.Models.User;
using PixivCS.Network;
using PixivCS.Utils;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PixivCS.Api;

/// <summary>
/// Pixiv App API 实现
/// </summary>
public partial class PixivAppApi : IDisposable
{
    /// <summary>
    /// 连接配置
    /// </summary>
    public ConnectionConfig Config { get; }
    private readonly PixivHttpClient _httpClient;
    private readonly ImageDownloader _imageDownloader;
    private const string BaseUrl = "https://app-api.pixiv.net";
    
    /// <summary>
    /// 当前认证结果，包含令牌信息和过期时间
    /// </summary>
    public AuthResult? CurrentAuthResult => _currentAuthResult;
    
    /// <summary>
    /// 检查当前是否已认证
    /// </summary>
    public bool IsAuthenticated => _currentAuthResult is { AccessToken: not null, IsExpired: false };
    
    /// <summary>
    /// 检查令牌是否需要刷新（即将过期或已过期）
    /// </summary>
    public bool ShouldRefreshToken => _currentAuthResult?.ShouldRefresh ?? true;

    private string? _accessToken;
    private AuthResult? _currentAuthResult;
    private string? _refreshToken;
    private bool _disposed = false;

    /// <summary>
    /// 初始化 PixivAppApi 实例
    /// </summary>
    /// <param name="config">连接配置，为 null 时使用默认配置</param>
    public PixivAppApi(ConnectionConfig? config = null)
    {
        Config = config ?? new ConnectionConfig();
        _httpClient = new PixivHttpClient(Config);
        _imageDownloader = new ImageDownloader(Config);
    }

    #region Authentication
    /// <summary>
    /// 使用刷新令牌进行认证
    /// </summary>
    public async Task<AuthResult> AuthAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        const string url = "https://oauth.secure.pixiv.net/auth/token";
        var formData = new Dictionary<string, string>
        {
            ["client_id"] = "MOBrBDS8blbauoSck0ZfDbtuzpyT",
            ["client_secret"] = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj",
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["get_secure_url"] = "1"
        };

        // 使用带有时间戳和哈希签名的认证头
        var headers = AuthHelper.CreateAuthHeaders();
        
        try
        {
            var result = await _httpClient.PostFormAsync<AuthResult>(url, formData, headers, cancellationToken);
            
            if (!string.IsNullOrEmpty(result.AccessToken))
            {
                // 保存认证信息
                _accessToken = result.AccessToken;
                _refreshToken = refreshToken;
                _currentAuthResult = result with { IssuedAt = DateTime.Now };
                
                return _currentAuthResult;
            }
            
            throw new PixivAuthException("Authentication failed: No access token received.");
        }
        catch (PixivParseException ex)
        {
            throw new PixivAuthException($"Authentication failed: {ex.Message}. Response: {ex.ResponseContent}", ex);
        }
        catch (PixivNetworkException ex)
        {
            throw new PixivAuthException($"Authentication network error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 设置访问令牌
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    public void SetAccessToken(string accessToken) => _accessToken = accessToken;

    /// <summary>
    /// 自动刷新令牌（如果需要）
    /// </summary>
    private async Task EnsureValidTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_currentAuthResult == null || string.IsNullOrEmpty(_refreshToken))
            return;
        if (!_currentAuthResult.ShouldRefresh)
            return;
        await AuthAsync(_refreshToken, cancellationToken);
    }
    #endregion

    #region User APIs
    /// <summary>
    /// 获取用户详细信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详细信息</returns>
    public async Task<UserDetail> GetUserDetailAsync(string userId, string filter = "for_ios", CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{BaseUrl}/v2/user/detail", new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["filter"] = filter
        });
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserDetail>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户的插画作品
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="type">作品类型，默认为插画</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户插画作品列表</returns>
    public async Task<UserIllusts> GetUserIllustsAsync(string userId, IllustType type = IllustType.Illust, string filter = "for_ios", string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["type"] = type.ToApiString(),
            ["filter"] = filter
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/illusts", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserIllusts>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户收藏的插画作品
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="restrict">可见性限制，默认为公开</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="maxBookmarkId">最大收藏ID，用于分页</param>
    /// <param name="tag">标签过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户收藏的插画作品列表</returns>
    public async Task<UserIllusts> GetUserBookmarksIllustAsync(string userId, RestrictType restrict = RestrictType.Public, string filter = "for_ios", string? maxBookmarkId = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{BaseUrl}/v1/user/bookmarks/illust", new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["restrict"] = restrict.ToApiString(),
            ["filter"] = filter,
            ["max_bookmark_id"] = maxBookmarkId,
            ["tag"] = tag
        });
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserIllusts>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户收藏的小说作品
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="restrict">可见性限制，默认为公开</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="maxBookmarkId">最大收藏ID，用于分页</param>
    /// <param name="tag">标签过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户收藏的小说作品列表</returns>
    public async Task<UserNovels> GetUserBookmarksNovelAsync(string userId, RestrictType restrict = RestrictType.Public, string filter = "for_ios", string? maxBookmarkId = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["restrict"] = restrict.ToApiString(),
            ["filter"] = filter
        };
        if (maxBookmarkId != null) parameters["max_bookmark_id"] = maxBookmarkId;
        if (tag != null) parameters["tag"] = tag;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/bookmarks/novel", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserNovels>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取与指定用户相关的用户列表
    /// </summary>
    /// <param name="seedUserId">种子用户ID</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>相关用户列表</returns>
    public async Task<UserFollowList> GetUserRelatedAsync(string seedUserId, string filter = "for_ios", string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["seed_user_id"] = seedUserId,
            ["filter"] = filter
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/related", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserFollowList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取推荐用户列表
    /// </summary>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐用户列表</returns>
    public async Task<UserFollowList> GetUserRecommendedAsync(string filter = "for_ios", string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["filter"] = filter
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/recommended", parameters);
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserFollowList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户关注的用户列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="restrict">可见性限制，默认为公开</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户关注列表</returns>
    public async Task<UserFollowList> GetUserFollowingAsync(string userId, RestrictType restrict = RestrictType.Public, string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["restrict"] = restrict.ToApiString()
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/following", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserFollowList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户的粉丝列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户粉丝列表</returns>
    public async Task<UserFollowList> GetUserFollowerAsync(string userId, string filter = "for_ios", string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["filter"] = filter
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/follower", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserFollowList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户的好P友列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户好P友列表</returns>
    public async Task<UserFollowList> GetUserMyPixivAsync(string userId, string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["user_id"] = userId
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/mypixiv", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserFollowList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    public async Task<UserList> GetUserListAsync(string userId, string filter = "for_ios", string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["filter"] = filter
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/list/{userId}", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户的小说作品
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户小说作品列表</returns>
    public async Task<UserNovels> GetUserNovelsAsync(string userId, string filter = "for_ios", string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["filter"] = filter
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/novels", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserNovels>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取用户收藏插画的标签列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="restrict">可见性限制，默认为公开</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户收藏标签列表</returns>
    public async Task<UserBookmarkTags> GetUserBookmarkTagsIllustAsync(string userId, RestrictType restrict = RestrictType.Public, string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["user_id"] = userId,
            ["restrict"] = restrict.ToApiString()
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/user/bookmark-tags/illust", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UserBookmarkTags>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 关注用户
    /// </summary>
    /// <param name="userId">要关注的用户ID</param>
    /// <param name="restrict">可见性限制，默认为公开</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="PixivAuthException">认证失败时抛出</exception>
    /// <exception cref="PixivNetworkException">网络错误时抛出</exception>
    public async Task AddUserFollowAsync(string userId, RestrictType restrict = RestrictType.Public, CancellationToken cancellationToken = default)
    {
        const string url = $"{BaseUrl}/v1/user/follow/add";
        var data = new Dictionary<string, string>
        {
            ["user_id"] = userId,
            ["restrict"] = restrict.ToApiString()
        };
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        await _httpClient.PostAsync<BaseResponse>(url, data, headers, cancellationToken);
    }

    /// <summary>
    /// 取消关注用户
    /// </summary>
    /// <param name="userId">要取消关注的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="PixivAuthException">认证失败时抛出</exception>
    /// <exception cref="PixivNetworkException">网络错误时抛出</exception>
    public async Task DeleteUserFollowAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string url = $"{BaseUrl}/v1/user/follow/delete";
        var data = new Dictionary<string, string>
        {
            ["user_id"] = userId
        };
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        await _httpClient.PostAsync<BaseResponse>(url, data, headers, cancellationToken);
    }

    /// <summary>
    /// 编辑用户AI作品显示设置
    /// </summary>
    /// <param name="setting">是否显示AI作品</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="PixivAuthException">认证失败时抛出</exception>
    /// <exception cref="PixivNetworkException">网络错误时抛出</exception>
    public async Task EditUserAiShowSettingsAsync(bool setting, CancellationToken cancellationToken = default)
    {
        const string url = $"{BaseUrl}/v1/user/ai-show-settings/edit";
        var data = new Dictionary<string, string>
        {
            ["setting"] = setting.ToApiString()
        };
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        await _httpClient.PostAsync<BaseResponse>(url, data, headers, cancellationToken);
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// 创建请求头
    /// </summary>
    private Dictionary<string, string> CreateHeaders(bool requireAuth = true)
    {
        var headers = new Dictionary<string, string>();
        
        if (requireAuth)
        {
            if (string.IsNullOrEmpty(_accessToken))
                throw new PixivAuthException("Access token is required but not set.");
            
            headers["Authorization"] = $"Bearer {_accessToken}";
        }
        
        return headers;
    }
    
    /// <summary>
    /// 创建请求头（异步版本，支持自动令牌刷新）
    /// </summary>
    private async Task<Dictionary<string, string>> CreateHeadersAsync(bool requireAuth = true, CancellationToken cancellationToken = default)
    {
        if (requireAuth)
        {
            await EnsureValidTokenAsync(cancellationToken);
        }
        
        return CreateHeaders(requireAuth);
    }

    /// <summary>
    /// 构建带查询参数的 URL
    /// </summary>
    private static string BuildUrl(string baseUrl, Dictionary<string, object?>? parameters = null)
    {
        if (parameters == null)
            return baseUrl;

        var queryParams = parameters
            .Where(kv => kv.Value != null && !string.IsNullOrEmpty(kv.Value.ToString()))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!.ToString()!)}")
            .ToArray();
        
        return queryParams.Length == 0 ? baseUrl : $"{baseUrl}?{string.Join("&", queryParams)}";
    }


    /// <summary>
    /// 格式化日期为 API 所需格式
    /// </summary>
    private static string? FormatDate(DateOnly? date)
    {
        return date?.ToString("yyyy-MM-dd");
    }
    #endregion

    #region Illust APIs
    /// <summary>
    /// 获取插画详细信息
    /// </summary>
    /// <param name="illustId">插画ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插画详细信息</returns>
    public async Task<IllustDetail> GetIllustDetailAsync(string illustId, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{BaseUrl}/v1/illust/detail", new Dictionary<string, object?> { ["illust_id"] = illustId });
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<IllustDetail>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取插画评论
    /// </summary>
    /// <param name="illustId">插画ID</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="includeTotalComments">是否包含评论总数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插画评论列表</returns>
    public async Task<IllustComments> GetIllustCommentsAsync(string illustId, string? offset = null, bool? includeTotalComments = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["illust_id"] = illustId
        };
        if (offset != null) parameters["offset"] = offset;
        if (includeTotalComments != null) parameters["include_total_comments"] = includeTotalComments.Value.ToApiString();
        
        var url = BuildUrl($"{BaseUrl}/v1/illust/comments", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<IllustComments>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取相关插画列表
    /// </summary>
    /// <param name="illustId">插画ID</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="seedIllustIds">种子插画ID数组</param>
    /// <param name="viewed">已查看的插画ID数组</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>相关插画列表</returns>
    public async Task<IllustList> GetIllustRelatedAsync(string illustId, string filter = "for_ios", string[]? seedIllustIds = null, string[]? viewed = null, string? offset = null, CancellationToken cancellationToken = default)
    {
        var urlParameters = new Dictionary<string, object?>
        {
            ["illust_id"] = illustId,
            ["filter"] = filter
        };
        if (offset != null) urlParameters["offset"] = offset;

        if (seedIllustIds is { Length: > 0 })
        {
            for (var i = 0; i < seedIllustIds.Length; i++)
            {
                urlParameters[$"seed_illust_ids[{i}]"] = seedIllustIds[i];
            }
        }

        if (viewed is { Length: > 0 })
        {
            for (var i = 0; i < viewed.Length; i++)
            {
                urlParameters[$"viewed[{i}]"] = viewed[i];
            }
        }

        var url = BuildUrl($"{BaseUrl}/v2/illust/related", urlParameters);
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<IllustList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取推荐插画列表
    /// </summary>
    /// <param name="contentType">内容类型，默认为插画</param>
    /// <param name="includeRankingLabel">是否包含排行榜标签</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="maxBookmarkIdForRecommend">推荐作品的最大收藏ID</param>
    /// <param name="minBookmarkIdForRecentIllust">最近插画的最小收藏ID</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="includeRankingIllusts">是否包含排行榜插画</param>
    /// <param name="bookmarkIllustIds">收藏插画ID数组</param>
    /// <param name="includePrivacyPolicy">是否包含隐私政策</param>
    /// <param name="viewed">已查看的插画ID数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐插画列表</returns>
    public async Task<IllustRecommended> GetIllustRecommendedAsync(ContentType contentType = ContentType.Illust, bool includeRankingLabel = true, string filter = "for_ios", string? maxBookmarkIdForRecommend = null, string? minBookmarkIdForRecentIllust = null, string? offset = null, bool? includeRankingIllusts = null, string[]? bookmarkIllustIds = null, bool? includePrivacyPolicy = null, string[]? viewed = null, CancellationToken cancellationToken = default)
    {
        var urlParameters = new Dictionary<string, object?>
        {
            ["content_type"] = contentType.ToApiString(),
            ["include_ranking_label"] = includeRankingLabel.ToApiString(),
            ["filter"] = filter
        };
        if (maxBookmarkIdForRecommend != null) urlParameters["max_bookmark_id_for_recommend"] = maxBookmarkIdForRecommend;
        if (minBookmarkIdForRecentIllust != null) urlParameters["min_bookmark_id_for_recent_illust"] = minBookmarkIdForRecentIllust;
        if (offset != null) urlParameters["offset"] = offset;
        if (includeRankingIllusts != null) urlParameters["include_ranking_illusts"] = includeRankingIllusts.Value.ToApiString();
        if (includePrivacyPolicy != null) urlParameters["include_privacy_policy"] = includePrivacyPolicy.Value.ToApiString();

        if (bookmarkIllustIds is { Length: > 0 })
        {
            for (var i = 0; i < bookmarkIllustIds.Length; i++)
            {
                urlParameters[$"bookmark_illust_ids[{i}]"] = bookmarkIllustIds[i];
            }
        }

        if (viewed is { Length: > 0 })
        {
            for (var i = 0; i < viewed.Length; i++)
            {
                urlParameters[$"viewed[{i}]"] = viewed[i];
            }
        }

        var url = BuildUrl($"{BaseUrl}/v1/illust/recommended", urlParameters);
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<IllustRecommended>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取关注用户的插画作品
    /// </summary>
    /// <param name="restrict">可见性限制，默认为公开</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关注用户的插画列表</returns>
    public async Task<IllustList> GetIllustFollowAsync(RestrictType restrict = RestrictType.Public, string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["restrict"] = restrict.ToApiString()
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v2/illust/follow", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<IllustList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取插画排行榜
    /// </summary>
    /// <param name="mode">排行榜模式，默认为日榜</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="date">指定日期</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插画排行榜列表</returns>
    public async Task<IllustList> GetIllustRankingAsync(RankingMode mode = RankingMode.Day, string filter = "for_ios", DateOnly? date = null, string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["mode"] = mode.ToApiString(),
            ["filter"] = filter
        };
        if (date != null) parameters["date"] = FormatDate(date)!;
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/illust/ranking", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<IllustList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取最新插画列表
    /// </summary>
    /// <param name="contentType">内容类型，默认为插画</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="maxIllustId">最大插画ID，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新插画列表</returns>
    public async Task<IllustList> GetIllustNewAsync(ContentType contentType = ContentType.Illust, string filter = "for_ios", string? maxIllustId = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["content_type"] = contentType.ToApiString(),
            ["filter"] = filter
        };
        if (maxIllustId != null) parameters["max_illust_id"] = maxIllustId;
        
        var url = BuildUrl($"{BaseUrl}/v1/illust/new", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<IllustList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取插画收藏详情
    /// </summary>
    /// <param name="illustId">插画ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插画收藏详情</returns>
    public async Task<IllustBookmarkDetail> GetIllustBookmarkDetailAsync(string illustId, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{BaseUrl}/v2/illust/bookmark/detail", new Dictionary<string, object?> { ["illust_id"] = illustId });
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<IllustBookmarkDetail>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 收藏插画
    /// </summary>
    /// <param name="illustId">插画ID</param>
    /// <param name="restrict">可见性限制，默认为公开</param>
    /// <param name="tags">收藏标签数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="PixivAuthException">认证失败时抛出</exception>
    /// <exception cref="PixivNetworkException">网络错误时抛出</exception>
    public async Task AddIllustBookmarkAsync(string illustId, RestrictType restrict = RestrictType.Public, string[]? tags = null, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["illust_id"] = illustId,
            ["restrict"] = restrict.ToApiString()
        };

        if (tags != null)
        {
            data["tags"] = tags;
        }

        const string url = $"{BaseUrl}/v2/illust/bookmark/add";
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        await _httpClient.PostAsync<BaseResponse>(url, data, headers, cancellationToken);
    }

    /// <summary>
    /// 取消收藏插画
    /// </summary>
    /// <param name="illustId">插画ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="PixivAuthException">认证失败时抛出</exception>
    /// <exception cref="PixivNetworkException">网络错误时抛出</exception>
    public async Task DeleteIllustBookmarkAsync(string illustId, CancellationToken cancellationToken = default)
    {
        const string url = $"{BaseUrl}/v1/illust/bookmark/delete";
        var data = new Dictionary<string, string>
        {
            ["illust_id"] = illustId
        };
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        await _httpClient.PostAsync<BaseResponse>(url, data, headers, cancellationToken);
    }

    /// <summary>
    /// 获取插画热门标签
    /// </summary>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插画热门标签列表</returns>
    public async Task<TrendingTagsIllust> GetTrendingTagsIllustAsync(string filter = "for_ios", CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{BaseUrl}/v1/trending-tags/illust", new Dictionary<string, object?> { ["filter"] = filter });
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<TrendingTagsIllust>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取动图元数据
    /// </summary>
    /// <param name="illustId">动图插画ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>动图元数据</returns>
    public async Task<UgoiraMetadata> GetUgoiraMetadataAsync(string illustId, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{BaseUrl}/v1/ugoira/metadata", new Dictionary<string, object?> { ["illust_id"] = illustId });
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<UgoiraMetadata>(url, headers, cancellationToken);
    }
    #endregion

    #region Novel APIs
    /// <summary>
    /// 获取小说详细信息
    /// </summary>
    /// <param name="novelId">小说ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>小说详细信息</returns>
    public async Task<NovelDetail> GetNovelDetailAsync(string novelId, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{BaseUrl}/v2/novel/detail", new Dictionary<string, object?> { ["novel_id"] = novelId });
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<NovelDetail>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取小说评论
    /// </summary>
    /// <param name="novelId">小说ID</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="includeTotalComments">是否包含评论总数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>小说评论列表</returns>
    public async Task<NovelComments> GetNovelCommentsAsync(string novelId, string? offset = null, bool? includeTotalComments = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["novel_id"] = novelId
        };
        if (offset != null) parameters["offset"] = offset;
        if (includeTotalComments != null) parameters["include_total_comments"] = includeTotalComments.Value.ToApiString();
        
        var url = BuildUrl($"{BaseUrl}/v1/novel/comments", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<NovelComments>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取推荐小说列表
    /// </summary>
    /// <param name="includeRankingLabel">是否包含排行榜标签</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="maxBookmarkIdForRecommend">推荐作品的最大收藏ID</param>
    /// <param name="minBookmarkIdForRecentNovel">最近小说的最小收藏ID</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="includeRankingNovels">是否包含排行榜小说</param>
    /// <param name="bookmarkNovelIds">收藏小说ID数组</param>
    /// <param name="includePrivacyPolicy">是否包含隐私政策</param>
    /// <param name="viewed">已查看的小说ID数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐小说列表</returns>
    public async Task<NovelRecommended> GetNovelRecommendedAsync(bool includeRankingLabel = true, string filter = "for_ios", string? maxBookmarkIdForRecommend = null, string? minBookmarkIdForRecentNovel = null, string? offset = null, bool? includeRankingNovels = null, string[]? bookmarkNovelIds = null, bool? includePrivacyPolicy = null, string[]? viewed = null, CancellationToken cancellationToken = default)
    {
        var urlParameters = new Dictionary<string, object?>
        {
            ["include_ranking_label"] = includeRankingLabel.ToApiString(),
            ["filter"] = filter
        };
        if (maxBookmarkIdForRecommend != null) urlParameters["max_bookmark_id_for_recommend"] = maxBookmarkIdForRecommend;
        if (minBookmarkIdForRecentNovel != null) urlParameters["min_bookmark_id_for_recent_novel"] = minBookmarkIdForRecentNovel;
        if (offset != null) urlParameters["offset"] = offset;
        if (includeRankingNovels != null) urlParameters["include_ranking_novels"] = includeRankingNovels.Value.ToApiString();
        if (includePrivacyPolicy != null) urlParameters["include_privacy_policy"] = includePrivacyPolicy.Value.ToApiString();

        if (bookmarkNovelIds is { Length: > 0 })
        {
            for (var i = 0; i < bookmarkNovelIds.Length; i++)
            {
                urlParameters[$"bookmark_novel_ids[{i}]"] = bookmarkNovelIds[i];
            }
        }

        if (viewed is { Length: > 0 })
        {
            for (var i = 0; i < viewed.Length; i++)
            {
                urlParameters[$"viewed[{i}]"] = viewed[i];
            }
        }

        var url = BuildUrl($"{BaseUrl}/v1/novel/recommended", urlParameters);
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<NovelRecommended>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取小说系列详情
    /// </summary>
    /// <param name="seriesId">小说系列ID</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="lastOrder">最后序号，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>小说系列详情</returns>
    public async Task<NovelSeriesDetail> GetNovelSeriesAsync(string seriesId, string filter = "for_ios", string? lastOrder = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["series_id"] = seriesId,
            ["filter"] = filter
        };
        if (lastOrder != null) parameters["last_order"] = lastOrder;
        
        var url = BuildUrl($"{BaseUrl}/v2/novel/series", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<NovelSeriesDetail>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取最新小说列表
    /// </summary>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="maxNovelId">最大小说ID，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新小说列表</returns>
    public async Task<NovelList> GetNovelNewAsync(string filter = "for_ios", string? maxNovelId = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["filter"] = filter
        };
        if (maxNovelId != null) parameters["max_novel_id"] = maxNovelId;
        
        var url = BuildUrl($"{BaseUrl}/v1/novel/new", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<NovelList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取关注用户的小说作品
    /// </summary>
    /// <param name="restrict">可见性限制，默认为公开</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关注用户的小说列表</returns>
    public async Task<NovelList> GetNovelFollowAsync(RestrictType restrict = RestrictType.Public, string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["restrict"] = restrict.ToApiString()
        };
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/novel/follow", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<NovelList>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 获取小说正文内容
    /// </summary>
    /// <param name="novelId">小说ID</param>
    /// <param name="raw">是否返回原始HTML响应，默认为false</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>小说正文内容</returns>
    /// <exception cref="PixivException">解析小说内容失败时抛出</exception>
    public async Task<WebviewNovel> GetNovelTextAsync(string novelId, bool raw = false, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"{BaseUrl}/webview/v2/novel", new Dictionary<string, object?>
        {
            ["id"] = novelId,
            ["viewer_version"] = "20221031_ai"
        });

        var headers = new Dictionary<string, string>();

        // 添加Bearer token认证
        if (!string.IsNullOrEmpty(_accessToken))
        {
            headers["Authorization"] = $"Bearer {_accessToken}";
        }

        // 获取HTML响应
        var htmlResponse = await _httpClient.GetStringAsync(url, headers, cancellationToken);
        
        if (raw)
        {
            // 如果需要原始响应，创建一个包含HTML的WebviewNovel对象
            return new WebviewNovel { NovelText = htmlResponse };
        }

        // 从HTML中提取JSON数据
        var match = NovelDataRegex().Match(htmlResponse);
        
        if (!match.Success || match.Groups.Count < 2)
        {
            throw new PixivException($"Extract novel content error: unable to find novel data in HTML response. HTML length: {htmlResponse.Length}");
        }

        var jsonContent = match.Groups[1].Value;
        
        try
        {
            var webviewNovel = JsonSerializer.Deserialize<WebviewNovel>(jsonContent, PixivJsonOptions.Webview) 
                ?? throw new PixivException("Failed to deserialize novel content");
            if (webviewNovel.NovelText == null && !string.IsNullOrEmpty(webviewNovel.Text))
            {
                webviewNovel.NovelText = webviewNovel.Text;
            }
            
            return webviewNovel;
        }
        catch (JsonException ex)
        {
            throw new PixivException($"Failed to parse novel JSON content: {ex.Message}");
        }
    }

    /// <summary>
    /// 从HTML中提取小说JSON数据的正则表达式
    /// </summary>
    [GeneratedRegex(@"novel:\s(\{.+\}),\s+isOwnWork", RegexOptions.Compiled)]
    private static partial Regex NovelDataRegex();
    #endregion

    #region Custom Request API

    /// <summary>
    /// 准备请求头的内部方法
    /// </summary>
    private Dictionary<string, string> PrepareRequestHeaders(bool requireAuth, Dictionary<string, string>? additionalHeaders = null)
    {
        var requestHeaders = new Dictionary<string, string>();
        
        // 添加认证头
        if (requireAuth)
        {
            var authHeaders = CreateHeaders();
            foreach (var header in authHeaders)
            {
                requestHeaders[header.Key] = header.Value;
            }
        }
        
        // 合并自定义头
        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                requestHeaders[header.Key] = header.Value;
            }
        }
        
        return requestHeaders;
    }
    
    /// <summary>
    /// 通用HTTP请求方法，支持GET和POST请求
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="method">HTTP方法，支持GET和POST</param>
    /// <param name="url">请求URL</param>
    /// <param name="requireAuth">是否需要认证，默认为true</param>
    /// <param name="headers">额外的请求头</param>
    /// <param name="data">POST请求数据</param>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>反序列化后的响应对象</returns>
    /// <exception cref="ArgumentException">不支持的HTTP方法时抛出</exception>
    public async Task<T> RequestCallAsync<T>(string method, string url, bool requireAuth = true, Dictionary<string, string>? headers = null, object? data = null, Dictionary<string, object?>? query = null, CancellationToken cancellationToken = default) where T : class
    {
        // 处理查询参数
        if (query != null)
        {
            url = BuildUrl(url, query);
        }
        
        // 确保令牌有效（如果需要认证）
        if (requireAuth)
        {
            await EnsureValidTokenAsync(cancellationToken);
        }
        
        var requestHeaders = PrepareRequestHeaders(requireAuth, headers);

        return method.ToUpperInvariant() switch
        {
            "GET" => await _httpClient.GetAsync<T>(url, requestHeaders, cancellationToken),
            "POST" => await _httpClient.PostAsync<T>(url, data, requestHeaders, cancellationToken),
            _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
        };
    }

    /// <summary>
    /// 通用HTTP请求方法，返回原始字符串响应
    /// </summary>
    /// <param name="method">HTTP方法，支持GET和POST</param>
    /// <param name="url">请求URL</param>
    /// <param name="requireAuth">是否需要认证，默认为true</param>
    /// <param name="headers">额外的请求头</param>
    /// <param name="data">POST请求数据</param>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>原始字符串响应</returns>
    /// <exception cref="ArgumentException">不支持的HTTP方法时抛出</exception>
    public async Task<string> RequestCallStringAsync(string method, string url, bool requireAuth = true, Dictionary<string, string>? headers = null, object? data = null, Dictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
    {
        // 处理查询参数
        if (query != null)
        {
            url = BuildUrl(url, query);
        }
        
        // 确保令牌有效（如果需要认证）
        if (requireAuth)
        {
            await EnsureValidTokenAsync(cancellationToken);
        }
        
        var requestHeaders = PrepareRequestHeaders(requireAuth, headers);

        return method.ToUpperInvariant() switch
        {
            "GET" => await _httpClient.GetStringAsync(url, requestHeaders, cancellationToken),
            "POST" => await _httpClient.PostStringAsync(url, data, requestHeaders, cancellationToken),
            _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
        };
    }
    #endregion

    #region Pagination API
    /// <summary>
    /// 根据next_url获取下一页数据
    /// </summary>
    /// <typeparam name="T">返回类型，必须继承自PaginatedResponse</typeparam>
    /// <param name="nextUrl">下一页的URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页数据</returns>
    /// <exception cref="ArgumentException">next_url为空时抛出</exception>
    public async Task<T> GetNextPageAsync<T>(string nextUrl, CancellationToken cancellationToken = default) where T : PaginatedResponse
    {
        if (string.IsNullOrWhiteSpace(nextUrl))
        {
            throw new ArgumentException("next_url 不能为空", nameof(nextUrl));
        }

        return await RequestCallAsync<T>("GET", nextUrl, requireAuth: true, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 根据当前页对象获取下一页数据
    /// </summary>
    /// <typeparam name="T">返回类型，必须继承自PaginatedResponse</typeparam>
    /// <param name="currentPage">当前页对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页数据，如果没有下一页则返回null</returns>
    /// <exception cref="ArgumentNullException">currentPage为null时抛出</exception>
    public async Task<T?> GetNextPageAsync<T>(T currentPage, CancellationToken cancellationToken = default) where T : PaginatedResponse
    {
        ArgumentNullException.ThrowIfNull(currentPage);

        if (!currentPage.HasNextPage())
        {
            return null;
        }

        return await GetNextPageAsync<T>(currentPage.NextUrl!, cancellationToken);
    }
    #endregion

    #region Search APIs
    /// <summary>
    /// 搜索插画
    /// </summary>
    /// <param name="word">搜索关键词</param>
    /// <param name="searchTarget">搜索目标，默认为标签部分匹配</param>
    /// <param name="sort">排序方式，默认为按时间降序</param>
    /// <param name="duration">时间范围</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插画搜索结果</returns>
    public async Task<SearchIllustResult> SearchIllustAsync(string word, SearchTarget searchTarget = SearchTarget.PartialMatchForTags, SortOrder sort = SortOrder.DateDesc, Duration? duration = null, string filter = "for_ios", string? offset = null, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["word"] = word,
            ["search_target"] = searchTarget.ToApiString(),
            ["sort"] = sort.ToApiString(),
            ["filter"] = filter
        };
        if (duration != null) parameters["duration"] = duration.ToApiString()!;
        if (offset != null) parameters["offset"] = offset;
        if (startDate != null) parameters["start_date"] = FormatDate(startDate)!;
        if (endDate != null) parameters["end_date"] = FormatDate(endDate)!;
        
        var url = BuildUrl($"{BaseUrl}/v1/search/illust", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<SearchIllustResult>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 搜索小说
    /// </summary>
    /// <param name="word">搜索关键词</param>
    /// <param name="searchTarget">搜索目标，默认为标签部分匹配</param>
    /// <param name="sort">排序方式，默认为按时间降序</param>
    /// <param name="duration">时间范围</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>小说搜索结果</returns>
    public async Task<SearchNovelResult> SearchNovelAsync(string word, SearchTarget searchTarget = SearchTarget.PartialMatchForTags, SortOrder sort = SortOrder.DateDesc, Duration? duration = null, string filter = "for_ios", string? offset = null, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["word"] = word,
            ["search_target"] = searchTarget.ToApiString(),
            ["sort"] = sort.ToApiString(),
            ["filter"] = filter
        };
        if (duration != null) parameters["duration"] = duration.ToApiString()!;
        if (offset != null) parameters["offset"] = offset;
        if (startDate != null) parameters["start_date"] = FormatDate(startDate)!;
        if (endDate != null) parameters["end_date"] = FormatDate(endDate)!;
        
        var url = BuildUrl($"{BaseUrl}/v1/search/novel", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<SearchNovelResult>(url, headers, cancellationToken);
    }

    /// <summary>
    /// 搜索用户
    /// </summary>
    /// <param name="word">搜索关键词</param>
    /// <param name="sort">排序方式，默认为按时间降序</param>
    /// <param name="duration">时间范围</param>
    /// <param name="filter">过滤器，默认为 "for_ios"</param>
    /// <param name="offset">偏移量，用于分页</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户搜索结果</returns>
    public async Task<SearchUserResult> SearchUserAsync(string word, SortOrder sort = SortOrder.DateDesc, Duration? duration = null, string filter = "for_ios", string? offset = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["word"] = word,
            ["sort"] = sort.ToApiString(),
            ["filter"] = filter
        };
        if (duration != null) parameters["duration"] = duration.ToApiString()!;
        if (offset != null) parameters["offset"] = offset;
        
        var url = BuildUrl($"{BaseUrl}/v1/search/user", parameters);
        
        var headers = await CreateHeadersAsync(cancellationToken: cancellationToken);
        return await _httpClient.GetAsync<SearchUserResult>(url, headers, cancellationToken);
    }
    #endregion

    #region Image Download APIs
    /// <summary>
    /// 下载图片为字节数组
    /// </summary>
    /// <param name="imageUrl">图片URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片字节数组</returns>
    public async Task<byte[]> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return await _imageDownloader.DownloadImageAsync(imageUrl, cancellationToken);
    }

    /// <summary>
    /// 下载图片并保存到文件
    /// </summary>
    /// <param name="imageUrl">图片URL</param>
    /// <param name="filePath">保存文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DownloadImageToFileAsync(string imageUrl, string filePath, CancellationToken cancellationToken = default)
    {
        await _imageDownloader.DownloadImageToFileAsync(imageUrl, filePath, cancellationToken);
    }

    /// <summary>
    /// 获取图片流
    /// </summary>
    /// <param name="imageUrl">图片URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片流</returns>
    public async Task<Stream> GetImageStreamAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return await _imageDownloader.GetImageStreamAsync(imageUrl, cancellationToken);
    }

    /// <summary>
    /// 批量下载图片
    /// </summary>
    /// <param name="imageUrls">图片URL集合</param>
    /// <param name="maxConcurrency">最大并发数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片URL与字节数组的字典</returns>
    public async Task<Dictionary<string, byte[]>> DownloadImagesAsync(IEnumerable<string> imageUrls, int? maxConcurrency = null, CancellationToken cancellationToken = default)
    {
        return await _imageDownloader.DownloadImagesAsync(imageUrls, maxConcurrency, cancellationToken);
    }

    /// <summary>
    /// 下载插画的所有图片
    /// </summary>
    /// <param name="illustDetail">插画详情</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片字节数组列表</returns>
    public async Task<List<byte[]>> DownloadIllustImagesAsync(IllustDetail illustDetail, CancellationToken cancellationToken = default)
    {
        if (illustDetail.Illust?.MetaPages is { Count: > 0 })
        {
            // 多页插画：优先下载原图，回退到大图
            var imageUrls = illustDetail.Illust.MetaPages
                .Select(page => page.ImageUrls?.Original ?? page.ImageUrls?.Large ?? "")
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();

            var results = await DownloadImagesAsync(imageUrls, null, cancellationToken);
            return [.. imageUrls.Select(url => results.GetValueOrDefault(url, []))];
        }
        else if (illustDetail.Illust?.MetaSinglePage?.OriginalImageUrl != null)
        {
            // 单页插画：下载原图
            var imageBytes = await DownloadImageAsync(illustDetail.Illust.MetaSinglePage.OriginalImageUrl, cancellationToken);
            return [imageBytes];
        }
        else if (illustDetail.Illust?.ImageUrls?.Original != null)
        {
            // 备选1：尝试主图片的原图
            var imageBytes = await DownloadImageAsync(illustDetail.Illust.ImageUrls.Original, cancellationToken);
            return [imageBytes];
        }
        else if (illustDetail.Illust?.ImageUrls?.Large != null)
        {
            // 备选2：下载大图
            var imageBytes = await DownloadImageAsync(illustDetail.Illust.ImageUrls.Large, cancellationToken);
            return [imageBytes];
        }

        return [];
    }
    #endregion

    #region Other APIs
    /// <summary>
    /// 获取特辑文章
    /// </summary>
    /// <param name="showcaseId">特辑ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>特辑文章信息</returns>
    public async Task<ShowcaseArticle> GetShowcaseArticleAsync(string showcaseId, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("https://www.pixiv.net/ajax/showcase/article", new Dictionary<string, object?> { ["article_id"] = showcaseId });

        // 特辑文章需要伪造浏览器请求头
        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            ["Referer"] = "https://www.pixiv.net/",
            ["Accept"] = "application/json"
        };

        return await _httpClient.GetAsync<ShowcaseArticle>(url, headers, cancellationToken);
    }
    #endregion

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _imageDownloader?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}