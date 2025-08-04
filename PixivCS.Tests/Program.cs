using PixivCS.Api;
using PixivCS.Exceptions;
using PixivCS.Models.Common;
using PixivCS.Network;
using PixivCS.Utils;

namespace PixivCS.Tests;

/// <summary>
/// PixivCS 基础功能测试程序
/// 演示三种连接方式和核心API功能
/// </summary>
class Program
{
    // 请在这里设置您的 refresh_token
    private const string REFRESH_TOKEN = "your_refresh_token"; // 请替换为您的实际 refresh_token
    
    // 代理设置（可选）
    private const string? PROXY_URL = null; // 例如: "http://127.0.0.1:7890"

    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== PixivCS 功能测试程序 ===");
        Console.WriteLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        if (string.IsNullOrEmpty(REFRESH_TOKEN))
        {
            Console.WriteLine("❌ 错误: 请在 Program.cs 中设置您的 REFRESH_TOKEN");
            Console.WriteLine("获取方法: https://github.com/eggplants/get-pixivpy-token");
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
            return;
        }

        await TestConnectionModes();
        Console.WriteLine();
        Console.WriteLine("=== 测试完成 ===");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 测试三种连接方式
    /// </summary>
    static async Task TestConnectionModes()
    {
        // 1. 普通连接
        await TestConnection("普通连接", new ConnectionConfig
        {
            Mode = ConnectionMode.Normal,
            TimeoutMs = 10000
        });

        // 2. 免代理直连
        await TestConnection("免代理直连", new ConnectionConfig
        {
            Mode = ConnectionMode.DirectBypass,
            TimeoutMs = 15000
        });

        // 3. 代理连接（如果配置了代理）
        if (!string.IsNullOrEmpty(PROXY_URL))
        {
            await TestConnection("代理连接", new ConnectionConfig
            {
                Mode = ConnectionMode.Proxy,
                ProxyUrl = PROXY_URL,
                TimeoutMs = 15000
            });
        }
        else
        {
            Console.WriteLine("🔸 代理连接测试 (跳过 - 未配置代理)");
            Console.WriteLine("".PadRight(50, '-'));
            Console.WriteLine("⚠️  如需测试代理功能，请在 Program.cs 中设置 PROXY_URL");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 测试指定连接方式的核心功能
    /// </summary>
    static async Task TestConnection(string testName, ConnectionConfig config)
    {
        Console.WriteLine($"🔸 {testName}测试");
        Console.WriteLine("".PadRight(50, '-'));

        using var api = new PixivAppApi(config);
        
        try
        {
            // 1. 认证测试
            Console.WriteLine("1. 正在测试认证...");
            var authResult = await api.AuthAsync(REFRESH_TOKEN);
            
            if (authResult.AccessToken != null)
            {
                Console.WriteLine("   ✅ 认证成功");
                Console.WriteLine($"   用户: {authResult.User?.Name} (ID: {authResult.UserId})");
            }
            else
            {
                Console.WriteLine("   ❌ 认证失败");
                return;
            }

            // 2. 插画功能测试
            Console.WriteLine("2. 正在测试插画功能...");
            var illustDetail = await api.GetIllustDetailAsync("133368512");
            
            if (illustDetail.Illust != null)
            {
                var illust = illustDetail.Illust;
                Console.WriteLine("   ✅ 插画详情获取成功");
                Console.WriteLine($"       标题: {illust.Title}");
                Console.WriteLine($"       作者: {illust.User?.Name}");
                Console.WriteLine($"       尺寸: {illust.Width}x{illust.Height}");
                Console.WriteLine($"       收藏: {illust.TotalBookmarks}");
            }
            else
            {
                Console.WriteLine("   ❌ 插画详情为空");
            }

            // 3. 小说功能测试
            Console.WriteLine("3. 正在测试小说功能...");
            var novelDetail = await api.GetNovelDetailAsync("12438689");
            
            if (novelDetail.Novel != null)
            {
                var novel = novelDetail.Novel;
                Console.WriteLine("   ✅ 小说详情获取成功");
                Console.WriteLine($"       标题: {novel.Title}");
                Console.WriteLine($"       作者: {novel.User?.Name}");
                Console.WriteLine($"       字数: {novel.TextLength}");
                Console.WriteLine($"       收藏: {novel.TotalBookmarks}");
                
                // 获取小说正文
                var novelText = await api.GetNovelTextAsync(novel.Id!.ToString());
                if (!string.IsNullOrEmpty(novelText.NovelText))
                {
                    Console.WriteLine($"       正文长度: {novelText.NovelText.Length} 字符");
                }
            }
            else
            {
                Console.WriteLine("   ❌ 小说详情为空");
            }

            // 4. 搜索功能测试
            Console.WriteLine("4. 正在测试搜索功能...");
            var searchResult = await api.SearchIllustAsync("初音ミク");
            
            if (searchResult.Illusts is { Count: > 0 })
            {
                Console.WriteLine("   ✅ 搜索结果获取成功");
                Console.WriteLine($"       结果数量: {searchResult.Illusts.Count}");
                Console.WriteLine($"       第一个作品: {searchResult.Illusts[0].Title}");
                Console.WriteLine($"       有下一页: {(searchResult.HasNextPage() ? "是" : "否")}");
            }
            else
            {
                Console.WriteLine("   ❌ 搜索结果为空");
            }

            // 5. 分页功能测试（使用官方账号 uid=11）
            Console.WriteLine("5. 正在测试分页功能 (pixiv事務局)...");
            await TestPaginationWithUser(api);

            Console.WriteLine($"🎉 {testName} 所有测试通过!");
        }
        catch (PixivAuthException ex)
        {
            Console.WriteLine($"❌ {testName} 认证错误: {ex.Message}");
        }
        catch (PixivNetworkException ex)
        {
            Console.WriteLine($"❌ {testName} 网络错误: {ex.Message}");
        }
        catch (PixivException ex)
        {
            Console.WriteLine($"❌ {testName} API错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {testName} 未知错误: {ex.Message}");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// 使用官方账号(pixiv事務局，uid=11)测试分页功能
    /// </summary>
    static async Task TestPaginationWithUser(PixivAppApi api)
    {
        try
        {
            // 获取用户11的插画作品
            var firstPage = await api.GetUserIllustsAsync("11", IllustType.Illust);
            
            if (firstPage.Illusts != null && firstPage.Illusts.Count > 0)
            {
                Console.WriteLine("   ✅ 用户11作品获取成功");
                Console.WriteLine($"       第1页作品数: {firstPage.Illusts.Count}");
                Console.WriteLine($"       最新作品: {firstPage.Illusts[0].Title}");
                
                // 演示推荐的分页方式：使用扩展方法
                if (firstPage.HasNextPage())
                {
                    var secondPage = await firstPage.GetNextPageAsync(api);
                    if (secondPage?.Illusts != null)
                    {
                        Console.WriteLine($"       第2页作品数: {secondPage.Illusts.Count}");
                        Console.WriteLine("       ✅ 分页功能正常");
                    }
                }
                else
                {
                    Console.WriteLine("       ℹ️  用户11只有1页作品");
                }
            }
            else
            {
                Console.WriteLine("   ❌ 用户11作品为空");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 分页测试失败: {ex.Message}");
        }
    }
}