using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PixivCS.Exceptions;

namespace PixivCS.Network;

/// <summary>
/// Pixiv 直连处理器（使用静态 IP 和低级TCP连接）
/// </summary>
public class PixivDirectHandler : HttpMessageHandler
{
    private readonly ConnectionConfig _config;
    private readonly IpHealthMonitor _healthMonitor;
    private readonly IpLoadBalancer _loadBalancer;

    /// <summary>
    /// 初始化Pixiv直连处理器
    /// </summary>
    /// <param name="config">连接配置</param>
    public PixivDirectHandler(ConnectionConfig config)
    {
        _config = config;
        _healthMonitor = new IpHealthMonitor(config);
        _loadBalancer = new IpLoadBalancer(config, _healthMonitor);
        config.Logger?.LogInfo($"PixivDirectHandler initialized with {config.StaticIpMapping.Count} IP mappings and load balancing");
    }

    /// <summary>
    /// 异步发送HTTP请求
    /// </summary>
    /// <param name="request">HTTP请求消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>HTTP响应消息</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
            throw new PixivNetworkException("Request URI cannot be null.");

        var host = request.RequestUri.Host;
        var port = request.RequestUri.Port == -1 ? (request.RequestUri.Scheme == "https" ? 443 : 80) : request.RequestUri.Port;
        var isHttps = request.RequestUri.Scheme == "https";

        var startTime = DateTime.UtcNow;
        Exception? lastException = null;

        // 尝试所有可用的IP地址
        var maxRetries = _config.EnableRetry ? _config.MaxRetries : 1;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var targetIp = _loadBalancer.SelectBestIp(host, _config.LoadBalanceStrategy);
            if (string.IsNullOrEmpty(targetIp))
            {
                throw new PixivNetworkException($"No IP address available for host: {host}");
            }

            _config.Logger?.LogDebug($"Attempting connection to {targetIp} for {host} (attempt {attempt + 1}/{maxRetries})");
            
            try
            {
                // 增加连接计数
                _loadBalancer.IncrementConnection(targetIp);
                
                var response = await ConnectAndSendAsync(targetIp, port, isHttps, request, host, cancellationToken);
                
                // 连接成功，更新健康状态和权重
                var responseTime = DateTime.UtcNow - startTime;
                _healthMonitor.MarkHealthy(targetIp);
                _loadBalancer.UpdateWeight(targetIp, true, responseTime);
                _loadBalancer.DecrementConnection(targetIp);
                
                _config.Logger?.LogDebug($"Successfully connected to {targetIp} for {host} in {responseTime.TotalMilliseconds}ms");
                return response;
            }
            catch (Exception ex)
            {
                _loadBalancer.DecrementConnection(targetIp);
                lastException = ex;
                
                // 标记IP为不健康
                _healthMonitor.MarkUnhealthy(targetIp, ex);
                _loadBalancer.UpdateWeight(targetIp, false, DateTime.UtcNow - startTime);
                
                _config.Logger?.LogWarning($"Connection failed to {targetIp} for {host}: {ex.Message}");
                
                // 如果是最后一次尝试，抛出异常
                if (attempt == maxRetries - 1)
                {
                    break;
                }
                
                // 等待一小段时间再重试
                await Task.Delay(100 * (attempt + 1), cancellationToken);
            }
        }

        throw new PixivNetworkException($"All connection attempts failed for host: {host}", lastException!);
    }

    /// <summary>
    /// 连接并发送请求
    /// </summary>
    private static async Task<HttpResponseMessage> ConnectAndSendAsync(string targetIp, int port, bool isHttps, 
        HttpRequestMessage request, string host, CancellationToken cancellationToken)
    {
        // 建立TCP连接
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(IPAddress.Parse(targetIp), port, cancellationToken);

        Stream stream = tcpClient.GetStream();

        // 如果是 HTTPS，建立 SSL 连接
        if (isHttps)
        {
            var sslStream = new SslStream(stream, false, ValidateServerCertificate);
            var sslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = "", // 设置为空字符串以规避SNI阻断
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                RemoteCertificateValidationCallback = ValidateServerCertificate
            };
            
            try
            {
                // 创建超时CancellationToken (10秒)
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                
                await sslStream.AuthenticateAsClientAsync(sslOptions, combinedCts.Token);
                stream = sslStream;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                throw new PixivNetworkException("SSL handshake timed out", ex);
            }
            catch (Exception ex)
            {
                throw new PixivNetworkException($"SSL authentication failed: {ex.Message}", ex);
            }
        }

        // 构建 HTTP 请求
        var httpRequest = await BuildHttpRequestAsync(request, host);
        var requestBytes = Encoding.UTF8.GetBytes(httpRequest);
        
        await stream.WriteAsync(requestBytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        // 读取响应
        var response = await ReadHttpResponseAsync(stream, cancellationToken);
        
        return response;
    }


    /// <summary>
    /// 验证服务器证书（允许pixiv.net证书，忽略SNI不匹配）
    /// </summary>
    private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, 
        X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        // 如果没有SSL错误，直接允许
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        // 对于直连到IP的情况，域名不匹配是正常的，只检查是否是Pixiv的证书
        if (certificate != null)
        {
            var subject = certificate.Subject.ToLowerInvariant();
            
            // 只要是pixiv.net相关的证书就允许
            if (subject.Contains("pixiv.net") || subject.Contains("*.pixiv.net"))
                return true;
        }

        // 对于直连IP的情况，域名不匹配错误是预期的，忽略它
        return sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
    }

    /// <summary>
    /// 构建 HTTP 请求字符串
    /// </summary>
    private static async Task<string> BuildHttpRequestAsync(HttpRequestMessage request, string host)
    {
        var method = request.Method.Method;
        var path = request.RequestUri?.PathAndQuery ?? "/";
        
        var requestBuilder = new StringBuilder();
        requestBuilder.AppendLine($"{method} {path} HTTP/1.1");
        requestBuilder.AppendLine($"Host: {host}");
        requestBuilder.AppendLine("Connection: close");
        
        // 添加请求头
        foreach (var header in request.Headers)
        {
            foreach (var value in header.Value)
            {
                requestBuilder.AppendLine($"{header.Key}: {value}");
            }
        }

        // 添加内容头和内容
        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                foreach (var value in header.Value)
                {
                    requestBuilder.AppendLine($"{header.Key}: {value}");
                }
            }

            var content = await request.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(content))
            {
                requestBuilder.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(content)}");
                requestBuilder.AppendLine();
                requestBuilder.Append(content);
            }
            else
            {
                requestBuilder.AppendLine();
            }
        }
        else
        {
            requestBuilder.AppendLine();
        }

        return requestBuilder.ToString();
    }

    /// <summary>
    /// 读取HTTP响应
    /// </summary>
    private static async Task<HttpResponseMessage> ReadHttpResponseAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var responseData = new List<byte>();
        var headerEndFound = false;
        var headerEndIndex = -1;

        // 读取响应头
        while (!headerEndFound)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                break;

            responseData.AddRange(buffer.Take(bytesRead));
            
            // 查找头部结束标记
            headerEndIndex = FindHeaderEnd([.. responseData]);
            
            if (headerEndIndex >= 0)
            {
                headerEndFound = true;
            }
        }

        // 继续读取剩余数据直到连接关闭
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                break;
            responseData.AddRange(buffer.Take(bytesRead));
        }

        return ParseHttpResponseFromBytes([.. responseData], headerEndIndex);
    }

    /// <summary>
    /// 在字节数组中查找HTTP头部结束位置
    /// </summary>
    private static int FindHeaderEnd(byte[] data)
    {
        var crlfcrlf = "\r\n\r\n"u8.ToArray(); // \r\n\r\n
        var lflf = "\n\n"u8.ToArray(); // \n\n

        // 先查找 \r\n\r\n
        for (var i = 0; i <= data.Length - 4; i++)
        {
            if (data[i] == crlfcrlf[0] && data[i + 1] == crlfcrlf[1] && 
                data[i + 2] == crlfcrlf[2] && data[i + 3] == crlfcrlf[3])
            {
                return i;
            }
        }

        // 再查找 \n\n
        for (var i = 0; i <= data.Length - 2; i++)
        {
            if (data[i] == lflf[0] && data[i + 1] == lflf[1])
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 从字节数组解析 HTTP 响应
    /// </summary>
    private static HttpResponseMessage ParseHttpResponseFromBytes(byte[] responseData, int headerEndIndex)
    {
        if (headerEndIndex < 0)
            throw new PixivNetworkException("Could not find HTTP header end.");

        // 解析头部
        var headerBytes = responseData.Take(headerEndIndex).ToArray();
        var headerText = Encoding.ASCII.GetString(headerBytes);
        var lines = headerText.Split(["\r\n", "\n"], StringSplitOptions.None);
        
        if (lines.Length == 0)
            throw new PixivNetworkException("Empty HTTP response.");

        // 解析状态行
        var statusLine = lines[0].Split(' ');
        if (statusLine.Length < 3)
            throw new PixivNetworkException($"Invalid HTTP status line: {lines[0]}");

        if (!int.TryParse(statusLine[1], out var statusCode))
            throw new PixivNetworkException($"Invalid HTTP status code: {statusLine[1]}");

        var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        var isChunked = false;
        var contentType = "application/octet-stream";

        // 解析头部字段
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
                break;

            var colonIndex = lines[i].IndexOf(':');
            if (colonIndex > 0)
            {
                var headerName = lines[i][..colonIndex].Trim();
                var headerValue = lines[i][(colonIndex + 1)..].Trim();
                
                if (headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    _ = int.TryParse(headerValue, out _);
                }
                else if (headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    isChunked = headerValue.Equals("chunked", StringComparison.OrdinalIgnoreCase);
                }
                else if (headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = headerValue;
                }
                else
                {
                    response.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
        }

        // 解析响应内容
        var headerEndBytes = headerText.Contains("\r\n\r\n") ? 4 : 2;
        var contentStartIndex = headerEndIndex + headerEndBytes;
        
        if (contentStartIndex < responseData.Length)
        {
            var contentBytes = responseData.Skip(contentStartIndex).ToArray();
            
            if (isChunked)
            {
                // 处理分块传输编码
                contentBytes = DecodeChunkedContentBytes(contentBytes);
            }
            
            response.Content = new ByteArrayContent(contentBytes);
            
            // 设置Content-Type
            if (!string.IsNullOrEmpty(contentType))
            {
                try
                {
                    response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
                }
                catch
                {
                    // 如果解析失败，使用默认的Content-Type
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                }
            }
        }

        return response;
    }

    /// <summary>
    /// 解码分块传输编码的内容
    /// </summary>
    private static byte[] DecodeChunkedContentBytes(byte[] chunkedData)
    {
        var result = new List<byte>();
        var position = 0;
        
        while (position < chunkedData.Length)
        {
            // 查找下一个 \r\n 或 \n（分块大小行的结束）
            var chunkSizeEnd = FindLineEnd(chunkedData, position);
            if (chunkSizeEnd == -1) break;
            
            // 读取分块大小行
            var chunkSizeBytes = chunkedData.Skip(position).Take(chunkSizeEnd - position).ToArray();
            var chunkSizeText = Encoding.ASCII.GetString(chunkSizeBytes).Trim();
            
            if (string.IsNullOrEmpty(chunkSizeText))
            {
                position = chunkSizeEnd + (chunkedData[chunkSizeEnd] == 0x0D ? 2 : 1); // 跳过 \r\n 或 \n
                continue;
            }
            
            // 解析分块大小（十六进制）
            if (int.TryParse(chunkSizeText, System.Globalization.NumberStyles.HexNumber, null, out var chunkSize))
            {
                if (chunkSize == 0)
                    break; // 结束分块
                
                // 移动到数据部分
                position = chunkSizeEnd + (chunkedData[chunkSizeEnd] == 0x0D ? 2 : 1);
                
                // 读取分块数据
                if (position + chunkSize <= chunkedData.Length)
                {
                    result.AddRange(chunkedData.Skip(position).Take(chunkSize));
                    position += chunkSize;
                    
                    // 跳过分块数据后的 \r\n
                    if (position < chunkedData.Length && chunkedData[position] == 0x0D)
                        position += 2; // \r\n
                    else if (position < chunkedData.Length && chunkedData[position] == 0x0A)
                        position += 1; // \n
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        
        return [.. result];
    }
    
    /// <summary>
    /// 查找行结束位置
    /// </summary>
    private static int FindLineEnd(byte[] data, int startIndex)
    {
        for (var i = startIndex; i < data.Length; i++)
        {
            if (data[i] == 0x0A) // \n
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 获取IP统计信息
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        return _loadBalancer.GetIpStatistics();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _healthMonitor?.Dispose();
        }
        base.Dispose(disposing);
    }
}