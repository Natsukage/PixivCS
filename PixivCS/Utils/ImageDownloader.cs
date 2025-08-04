using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using PixivCS.Exceptions;
using PixivCS.Network;
using PixivCS.Models.Common;

namespace PixivCS.Utils;

/// <summary>
/// Pixiv 图片下载工具类
/// </summary>
public partial class ImageDownloader : IDisposable
{
    private readonly ConnectionConfig _config;
    private readonly HttpClient _httpClient;
    private readonly IPixivLogger _logger;
    private readonly SemaphoreSlim _semaphore = new(DefaultMaxConcurrency);
    private static readonly int DefaultMaxConcurrency = Environment.ProcessorCount * 2;
    private bool _disposed = false;

    /// <summary>
    /// 初始化图片下载器
    /// </summary>
    /// <param name="config">连接配置</param>
    /// <param name="logger">日志记录器</param>
    public ImageDownloader(ConnectionConfig config, IPixivLogger? logger = null)
    {
        _config = config;
        _logger = logger ?? config.Logger ?? NullPixivLogger.Instance;
        _httpClient = CreateHttpClient();
    }

    /// <summary>
    /// 创建用于图片下载的 HttpClient
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        HttpMessageHandler handler = _config.Mode switch
        {
            ConnectionMode.Proxy => CreateProxyHandler(),
            // DirectBypass 和 Normal 模式都使用标准 HttpClientHandler
            // 因为图片下载在 DirectBypass 模式下通过 HTTP + IP 地址访问
            _ => new HttpClientHandler()
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(_config.TimeoutMs)
        };

        return client;
    }

    /// <summary>
    /// 匹配 pximg.net 图片URL的正则表达式
    /// </summary>
    [GeneratedRegex(@"https?://([is])\.pximg\.net/(.+)", RegexOptions.Compiled)]
    private static partial Regex PximgUrlRegex();

    /// <summary>
    /// 下载单张图片并返回字节数组
    /// </summary>
    public async Task<byte[]> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        var directUrl = ConvertToDirectUrl(imageUrl);
        var headers = CreateImageHeaders(imageUrl);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await DownloadWithRetryAsync(directUrl, headers, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 下载图片到指定文件路径
    /// </summary>
    public async Task DownloadImageToFileAsync(string imageUrl, string filePath, CancellationToken cancellationToken = default)
    {
        var imageBytes = await DownloadImageAsync(imageUrl, cancellationToken);
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
    }

    /// <summary>
    /// 获取图片流
    /// </summary>
    public async Task<Stream> GetImageStreamAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        var imageBytes = await DownloadImageAsync(imageUrl, cancellationToken);
        return new MemoryStream(imageBytes);
    }

    /// <summary>
    /// 批量下载图片（并发）
    /// </summary>
    public async Task<Dictionary<string, byte[]>> DownloadImagesAsync(IEnumerable<string> imageUrls, 
        int? maxConcurrency = null, CancellationToken cancellationToken = default)
    {
        var urls = imageUrls.ToList();
        var concurrency = maxConcurrency ?? DefaultMaxConcurrency;
        var semaphore = new SemaphoreSlim(concurrency);
        var results = new ConcurrentDictionary<string, byte[]>();

        var tasks = urls.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var imageBytes = await DownloadImageAsync(url, cancellationToken);
                results[url] = imageBytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to download image {url}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return new Dictionary<string, byte[]>(results);
    }

    /// <summary>
    /// 将 Pixiv 图片 URL 转换为直连 URL
    /// </summary>
    private string ConvertToDirectUrl(string originalUrl)
    {
        // 如果不是 i.pximg.net 或 s.pximg.net 的 URL，直接返回
        var match = PximgUrlRegex().Match(originalUrl);
        if (!match.Success)
            return originalUrl;

        var subdomain = match.Groups[1].Value; // "i" 或 "s"
        var path = match.Groups[2].Value;
        var domain = $"{subdomain}.pximg.net";

        // 根据连接模式处理
        return _config.Mode switch
        {
            ConnectionMode.DirectBypass => ConvertToDirectBypassUrl(domain, path),
            _ => originalUrl // 普通模式和代理模式直接使用原URL
        };
    }

    /// <summary>
    /// 转换为免代理直连 URL（带负载均衡）
    /// </summary>
    private string ConvertToDirectBypassUrl(string domain, string path)
    {
        // 在免代理直连模式下，选择最佳IP地址，使用HTTP协议
        var selectedIp = SelectBestIpForDomain(domain);
        if (!string.IsNullOrEmpty(selectedIp))
        {
            return $"http://{selectedIp}/{path}";
        }
        
        // 如果没有找到IP映射，fallback到原始域名
        return $"http://{domain}/{path}";
    }

    /// <summary>
    /// 为指定域名选择最佳IP地址（负载均衡核心逻辑）
    /// </summary>
    private string? SelectBestIpForDomain(string domain)
    {
        if (!_config.StaticIpMapping.TryGetValue(domain, out var ips) || ips.Count == 0)
            return null;

        // 这里实现负载均衡策略
        return _config.LoadBalanceStrategy switch
        {
            LoadBalanceStrategy.RoundRobin => SelectRoundRobinIp(domain, ips),
            LoadBalanceStrategy.HealthyFirst => SelectHealthyFirstIp(ips),
            _ => ips[0] // 默认使用第一个IP
        };
    }

    // 线程安全的轮询计数器
    private static readonly Dictionary<string, int> RoundRobinCounters = [];
    private static readonly object CounterLock = new();

    /// <summary>
    /// 轮询选择IP
    /// </summary>
    private string SelectRoundRobinIp(string domain, List<string> ips)
    {
        lock (CounterLock)
        {
            if (!RoundRobinCounters.TryGetValue(domain, out var counter))
            {
                counter = 0;
            }
            
            var selectedIp = ips[counter % ips.Count];
            RoundRobinCounters[domain] = (counter + 1) % ips.Count;
            
            _logger.LogDebug($"Round-robin selected IP {selectedIp} for {domain} (index: {counter % ips.Count})");
            return selectedIp;
        }
    }

    /// <summary>
    /// 健康优先选择IP
    /// </summary>
    private string SelectHealthyFirstIp(List<string> ips)
    {
        // 目前简单返回第一个，后续可以集成健康检查
        var selectedIp = ips[0];
        _logger.LogDebug($"Healthy-first selected IP {selectedIp} (first available)");
        return selectedIp;
    }

    /// <summary>
    /// 创建图片请求的HTTP头
    /// </summary>
    private static Dictionary<string, string> CreateImageHeaders(string originalUrl)
    {
        // 从原URL中提取域名作为Host头
        var uri = new Uri(originalUrl);
        var host = uri.Host;

        return new Dictionary<string, string>
        {
            ["Host"] = host,
            ["Referer"] = "https://www.pixiv.net/",
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36"
        };
    }

    /// <summary>
    /// 带重试机制的下载
    /// </summary>
    private async Task<byte[]> DownloadWithRetryAsync(string url, Dictionary<string, string> headers, 
        CancellationToken cancellationToken)
    {
        var maxRetries = _config.MaxRetries;
        var retryDelays = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };
        
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // 添加请求头
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken);
                }

                throw response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Forbidden => new PixivNetworkException($"Access forbidden (403) for image: {url}"),
                    System.Net.HttpStatusCode.NotFound => new PixivNetworkException($"Image not found (404): {url}"),
                    _ => new PixivNetworkException($"HTTP {(int)response.StatusCode} ({response.StatusCode}) for image: {url}")
                };
            }
            catch (PixivException)
            {
                throw; // 直接抛出业务异常，不重试
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogDebug($"Image download attempt {attempt + 1} failed for {url}: {ex.Message}");
                
                // 等待后重试
                var delay = attempt < retryDelays.Length ? retryDelays[attempt] : retryDelays.Last();
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new PixivNetworkException($"Failed to download image after {maxRetries + 1} attempts: {url}");
    }

    /// <summary>
    /// 创建代理处理器
    /// </summary>
    private HttpClientHandler CreateProxyHandler()
    {
        if (string.IsNullOrEmpty(_config.ProxyUrl))
            throw new PixivNetworkException("Proxy URL is required when using proxy mode.");

        return new HttpClientHandler
        {
            Proxy = new System.Net.WebProxy(_config.ProxyUrl),
            UseProxy = true
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _semaphore?.Dispose();
                _httpClient?.Dispose();
            }
            _disposed = true;
        }
    }
}