using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using PixivCS.Exceptions;
using PixivCS.Models.Common;
using PixivCS.Utils;

namespace PixivCS.Network;

/// <summary>
/// Pixiv 专用 HTTP 客户端
/// </summary>
public class PixivHttpClient : IDisposable
{
    private readonly ConnectionConfig _config;
    private readonly HttpClient _httpClient;
    private readonly IPixivLogger _logger;
    private readonly Dictionary<string, string> _cachedIps = [];
    private bool _disposed = false;
    
    /// <summary>
    /// 启用调试模式，将保存原始JSON响应到logs目录
    /// </summary>
    public static bool EnableDebugMode { get; set; } = false;

    /// <summary>
    /// 初始化Pixiv HTTP客户端
    /// </summary>
    /// <param name="config">连接配置</param>
    /// <param name="logger">日志记录器</param>
    public PixivHttpClient(ConnectionConfig config, IPixivLogger? logger = null)
    {
        _config = config;
        _logger = logger ?? config.Logger ?? NullPixivLogger.Instance;
        _httpClient = CreateHttpClient();
    }

    /// <summary>
    /// 创建 HTTP 客户端
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        // Creating HTTP client with connection mode: {_config.Mode}

        HttpMessageHandler handler = _config.Mode switch
        {
            ConnectionMode.Normal => new HttpClientHandler(),
            ConnectionMode.Proxy => CreateProxyHandler(),
            ConnectionMode.DirectBypass => CreateBypassHandler(),
            _ => new HttpClientHandler()
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(_config.TimeoutMs)
        };

        // 设置默认请求头
        client.DefaultRequestHeaders.Add("user-agent", "PixivIOSApp/7.13.3 (iOS 14.6; iPhone13,2)");
        client.DefaultRequestHeaders.Add("app-os", "ios");
        client.DefaultRequestHeaders.Add("app-os-version", "14.6");

        return client;
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
            Proxy = new WebProxy(_config.ProxyUrl),
            UseProxy = true
        };
    }

    /// <summary>
    /// 创建免代理处理器
    /// </summary>
    private PixivDirectHandler CreateBypassHandler() => new (_config);

    /// <summary>
    /// 发送 GET 请求
    /// </summary>
    public async Task<T> GetAsync<T>(string url, Dictionary<string, string>? headers = null, 
        CancellationToken cancellationToken = default) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var response = await SendWithRetryAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // 保存调试JSON
        if (EnableDebugMode)
        {
            await SaveDebugJsonAsync(url, "GET", content);
        }
        
        try
        {
            var result = JsonSerializer.Deserialize<T>(content, PixivJsonOptions.Web) ?? throw new PixivParseException("Deserialized result is null.", content);

            // 检查是否为 BaseResponse 类型，如果是则检查错误
            if (result is BaseResponse { HasError: true } baseResponse)
            {
                throw new PixivException($"API 错误：{baseResponse.GetErrorMessage()}");
            }
            
            return result;
        }
        catch (JsonException ex)
        {
            throw new PixivParseException("Failed to deserialize response.", content, ex);
        }
    }

    /// <summary>
    /// 获取字符串响应（用于HTML等非JSON内容）
    /// </summary>
    public async Task<string> GetStringAsync(string url, Dictionary<string, string>? headers = null, 
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var response = await SendWithRetryAsync(request, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// 发送 POST 请求（JSON 数据）
    /// </summary>
    public async Task<T> PostAsync<T>(string url, object? data = null, 
        Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        
        if (data != null)
        {
            var json = JsonSerializer.Serialize(data, PixivJsonOptions.Web);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var response = await SendWithRetryAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // 保存调试JSON
        if (EnableDebugMode)
        {
            await SaveDebugJsonAsync(url, "POST", content);
        }
        
        try
        {
            var result = JsonSerializer.Deserialize<T>(content, PixivJsonOptions.Web) ?? throw new PixivParseException("Deserialized result is null.", content);

            // 检查是否为 BaseResponse 类型，如果是则检查错误
            if (result is BaseResponse { HasError: true } baseResponse)
            {
                throw new PixivException($"API 错误：{baseResponse.GetErrorMessage()}");
            }
            
            return result;
        }
        catch (JsonException ex)
        {
            throw new PixivParseException("Failed to deserialize response.", content, ex);
        }
    }

    /// <summary>
    /// 发送 POST 请求并返回字符串响应
    /// </summary>
    public async Task<string> PostStringAsync(string url, object? data = null, 
        Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        
        if (data != null)
        {
            var json = JsonSerializer.Serialize(data, PixivJsonOptions.Web);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var response = await SendWithRetryAsync(request, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// 发送 POST 请求（表单数据）
    /// </summary>
    public async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> formData, 
        Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        
        // 创建表单内容
        var formContent = new FormUrlEncodedContent(formData);
        request.Content = formContent;

        if (headers != null)
        {
            foreach (var header in headers)
            {
                // Content-Type 需要特殊处理
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    request.Content.Headers.ContentType = 
                        new System.Net.Http.Headers.MediaTypeHeaderValue(header.Value);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        var response = await SendWithRetryAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // 保存调试JSON
        if (EnableDebugMode)
        {
            await SaveDebugJsonAsync(url, "POST_FORM", content);
        }
        
        try
        {
            var result = JsonSerializer.Deserialize<T>(content, PixivJsonOptions.Web) ?? throw new PixivParseException("Deserialized result is null.", content);

            // 检查是否为 BaseResponse 类型，如果是则检查错误
            if (result is BaseResponse { HasError: true } baseResponse)
            {
                throw new PixivException($"API 错误：{baseResponse.GetErrorMessage()}");
            }
            
            return result;
        }
        catch (JsonException ex)
        {
            throw new PixivParseException("Failed to deserialize response.", content, ex);
        }
    }

    /// <summary>
    /// 带重试机制的请求发送
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts <= _config.MaxRetries)
        {
            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                    return response;

                // 处理特定的 HTTP 错误
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        throw new PixivAuthException($"Unauthorized (401). Please check your access token. Response: {responseContent}");
                    case HttpStatusCode.BadRequest:
                        throw new PixivNetworkException($"Bad Request (400). Check your request parameters. Response: {responseContent}");
                    case HttpStatusCode.TooManyRequests:
                    {
                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                        throw new PixivRateLimitException($"Rate limit exceeded. Response: {responseContent}", retryAfter);
                    }
                    default:
                        throw new PixivNetworkException(
                            $"HTTP request failed with status {(int)response.StatusCode} ({response.StatusCode}). Response: {responseContent}");
                }
            }
            catch (Exception ex) when (ex is not PixivException)
            {
                lastException = ex;
                attempts++;
                
                if (attempts <= _config.MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts)); // 指数退避
                    await Task.Delay(delay, cancellationToken);
                    
                    // 克隆请求用于重试
                    request = await CloneRequestAsync(request);
                }
            }
        }

        throw new PixivNetworkException("Request failed after all retry attempts.", lastException!);
    }

    /// <summary>
    /// 克隆 HTTP 请求消息
    /// </summary>
    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        
        if (original.Content != null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    /// <summary>
    /// 保存调试JSON到logs目录
    /// </summary>
    private async Task SaveDebugJsonAsync(string url, string method, string jsonContent)
    {
        try
        {
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.Trim('/').Split('/');
            var endpoint = string.Join("_", pathParts.Where(p => !string.IsNullOrEmpty(p)));
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{method}_{endpoint}_{timestamp}.json";
            
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(logsDir);
            
            var filePath = Path.Combine(logsDir, fileName);
            await File.WriteAllTextAsync(filePath, jsonContent, Encoding.UTF8);
            
            _logger.LogDebug($"Saved JSON response to: {fileName}");
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Failed to save JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试连接
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://www.pixiv.net/", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// JSON 序列化选项
/// </summary>
public static class PixivJsonOptions
{
    /// <summary>
    /// Web API用的JSON序列化选项
    /// </summary>
    public static readonly JsonSerializerOptions Web = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Webview API用的JSON序列化选项
    /// </summary>
    public static readonly JsonSerializerOptions Webview = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}