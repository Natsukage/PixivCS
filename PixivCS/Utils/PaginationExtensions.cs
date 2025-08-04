using PixivCS.Api;
using PixivCS.Models.Common;
using PixivCS.Models.Illust;
using PixivCS.Models.Novel;
using PixivCS.Models.Search;
using PixivCS.Models.User;

namespace PixivCS.Utils;

/// <summary>
/// 分页功能扩展方法
/// </summary>
public static class PaginationExtensions
{

    // === 新的直观方法：currentPage.GetNextPageAsync(api) ===
    
    /// <summary>
    /// 获取用户插画作品的下一页
    /// </summary>
    /// <param name="currentPage">当前页响应</param>
    /// <param name="api">API 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页的用户插画数据，如果没有下一页则返回 null</returns>
    public static async Task<UserIllusts?> GetNextPageAsync(this UserIllusts currentPage, PixivAppApi api, CancellationToken cancellationToken = default)
    {
        if (!currentPage.HasNextPage())
            return null;

        return await api.GetNextPageAsync<UserIllusts>(currentPage.NextUrl!, cancellationToken);
    }

    /// <summary>
    /// 获取用户小说作品的下一页
    /// </summary>
    /// <param name="currentPage">当前页响应</param>
    /// <param name="api">API 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页的用户小说数据，如果没有下一页则返回 null</returns>
    public static async Task<UserNovels?> GetNextPageAsync(this UserNovels currentPage, PixivAppApi api, CancellationToken cancellationToken = default)
    {
        if (!currentPage.HasNextPage())
            return null;

        return await api.GetNextPageAsync<UserNovels>(currentPage.NextUrl!, cancellationToken);
    }

    /// <summary>
    /// 获取插画搜索结果的下一页
    /// </summary>
    /// <param name="currentPage">当前页响应</param>
    /// <param name="api">API 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页的搜索结果，如果没有下一页则返回 null</returns>
    public static async Task<SearchIllustResult?> GetNextPageAsync(this SearchIllustResult currentPage, PixivAppApi api, CancellationToken cancellationToken = default)
    {
        if (!currentPage.HasNextPage())
            return null;

        return await api.GetNextPageAsync<SearchIllustResult>(currentPage.NextUrl!, cancellationToken);
    }

    /// <summary>
    /// 获取小说搜索结果的下一页
    /// </summary>
    /// <param name="currentPage">当前页响应</param>
    /// <param name="api">API 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页的搜索结果，如果没有下一页则返回 null</returns>
    public static async Task<SearchNovelResult?> GetNextPageAsync(this SearchNovelResult currentPage, PixivAppApi api, CancellationToken cancellationToken = default)
    {
        if (!currentPage.HasNextPage())
            return null;

        return await api.GetNextPageAsync<SearchNovelResult>(currentPage.NextUrl!, cancellationToken);
    }

    /// <summary>
    /// 获取用户搜索结果的下一页
    /// </summary>
    /// <param name="currentPage">当前页响应</param>
    /// <param name="api">API 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页的搜索结果，如果没有下一页则返回 null</returns>
    public static async Task<SearchUserResult?> GetNextPageAsync(this SearchUserResult currentPage, PixivAppApi api, CancellationToken cancellationToken = default)
    {
        if (!currentPage.HasNextPage())
            return null;

        return await api.GetNextPageAsync<SearchUserResult>(currentPage.NextUrl!, cancellationToken);
    }

    /// <summary>
    /// 获取插画列表的下一页
    /// </summary>
    /// <param name="currentPage">当前页响应</param>
    /// <param name="api">API 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页的插画列表，如果没有下一页则返回 null</returns>
    public static async Task<IllustList?> GetNextPageAsync(this IllustList currentPage, PixivAppApi api, CancellationToken cancellationToken = default)
    {
        if (!currentPage.HasNextPage())
            return null;

        return await api.GetNextPageAsync<IllustList>(currentPage.NextUrl!, cancellationToken);
    }

    /// <summary>
    /// 获取小说列表的下一页
    /// </summary>
    /// <param name="currentPage">当前页响应</param>
    /// <param name="api">API 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页的小说列表，如果没有下一页则返回 null</returns>
    public static async Task<NovelList?> GetNextPageAsync(this NovelList currentPage, PixivAppApi api, CancellationToken cancellationToken = default)
    {
        if (!currentPage.HasNextPage())
            return null;

        return await api.GetNextPageAsync<NovelList>(currentPage.NextUrl!, cancellationToken);
    }

    /// <summary>
    /// 获取用户关注列表的下一页
    /// </summary>
    /// <param name="currentPage">当前页响应</param>
    /// <param name="api">API 实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下一页的用户关注列表，如果没有下一页则返回 null</returns>
    public static async Task<UserFollowList?> GetNextPageAsync(this UserFollowList currentPage, PixivAppApi api, CancellationToken cancellationToken = default)
    {
        if (!currentPage.HasNextPage())
            return null;

        return await api.GetNextPageAsync<UserFollowList>(currentPage.NextUrl!, cancellationToken);
    }
}