using PixivCS.Models.Common;
using PixivCS.Utils;

namespace PixivCS.Network;

/// <summary>
/// 连接模式配置
/// </summary>
public record ConnectionConfig
{
    /// <summary>
    /// 连接模式
    /// </summary>
    public ConnectionMode Mode { get; init; } = ConnectionMode.Normal;

    /// <summary>
    /// 代理地址（当 Mode 为 Proxy 时使用）
    /// </summary>
    public string? ProxyUrl { get; init; }


    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int TimeoutMs { get; init; } = 30000; // 增加到30秒，适应不同网络环境

    /// <summary>
    /// 是否启用重试机制
    /// </summary>
    public bool EnableRetry { get; init; } = true;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// 静态 IP 地址列表（当 Mode 为 DirectBypass 时使用）
    /// </summary>
    public Dictionary<string, List<string>> StaticIpMapping { get; init; } = GetDefaultIpMapping();

    /// <summary>
    /// 日志记录器（可选，默认为 null 表示无日志输出）
    /// </summary>
    public IPixivLogger? Logger { get; init; }

    /// <summary>
    /// 负载均衡策略
    /// </summary>
    public LoadBalanceStrategy LoadBalanceStrategy { get; init; } = LoadBalanceStrategy.HealthyFirst;


    /// <summary>
    /// 获取默认的 IP 映射配置
    /// </summary>
    private static Dictionary<string, List<string>> GetDefaultIpMapping() => new()
    {
        ["www.pixiv.net"] = ["210.140.139.155"],
        ["app-api.pixiv.net"] = ["210.140.139.155"],
        ["oauth.secure.pixiv.net"] = ["210.140.139.155"],
        ["i.pximg.net"] = ["210.140.139.132", "210.140.139.137", "210.140.139.130", "210.140.139.133", "210.140.139.135", "210.140.139.136", "210.140.139.129", "210.140.139.134", "210.140.139.131"],
        ["s.pximg.net"] = ["210.140.139.132", "210.140.139.137", "210.140.139.130", "210.140.139.133", "210.140.139.135", "210.140.139.136", "210.140.139.129", "210.140.139.134", "210.140.139.131"]
    };

}