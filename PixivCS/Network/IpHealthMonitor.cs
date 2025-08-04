using System.Collections.Concurrent;
using System.Net.NetworkInformation;

namespace PixivCS.Network;

/// <summary>
/// IP地址健康状态监控器
/// </summary>
public class IpHealthMonitor
{
    private readonly ConcurrentDictionary<string, IpHealthInfo> _ipHealth = new();
    private readonly Timer _healthCheckTimer;
    private readonly ConnectionConfig _config;

    /// <summary>
    /// 初始化IP健康状态监控器
    /// </summary>
    /// <param name="config">连接配置</param>
    public IpHealthMonitor(ConnectionConfig config)
    {
        _config = config;
        
        // 初始化所有IP为健康状态
        foreach (var (host, ips) in config.StaticIpMapping)
        {
            foreach (var ip in ips)
            {
                _ipHealth[ip] = new IpHealthInfo { IsHealthy = true };
            }
        }

        // 启动健康检查定时器（每30秒检查一次）
        _healthCheckTimer = new Timer(PerformHealthCheck, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 标记IP为不健康状态
    /// </summary>
    public void MarkUnhealthy(string ip, Exception? exception = null)
    {
        if (_ipHealth.TryGetValue(ip, out var health))
        {
            health.IsHealthy = false;
            health.LastFailureTime = DateTime.UtcNow;
            health.FailureCount++;
            health.LastException = exception;
            
            _config.Logger?.LogWarning($"IP {ip} marked as unhealthy. Failure count: {health.FailureCount}");
        }
    }

    /// <summary>
    /// 标记IP为健康状态
    /// </summary>
    public void MarkHealthy(string ip)
    {
        if (_ipHealth.TryGetValue(ip, out var health))
        {
            var wasUnhealthy = !health.IsHealthy;
            health.IsHealthy = true;
            health.FailureCount = 0;
            health.LastSuccessTime = DateTime.UtcNow;
            health.LastException = null;
            
            if (wasUnhealthy)
            {
                _config.Logger?.LogInfo($"IP {ip} restored to healthy state");
            }
        }
    }

    /// <summary>
    /// 获取主机的健康IP列表
    /// </summary>
    public List<string> GetHealthyIps(string host)
    {
        if (!_config.StaticIpMapping.TryGetValue(host, out var ips))
            return [];

        var healthyIps = new List<string>();
        var unhealthyIps = new List<string>();

        foreach (var ip in ips)
        {
            if (_ipHealth.TryGetValue(ip, out var health))
            {
                if (health.IsHealthy)
                {
                    healthyIps.Add(ip);
                }
                else if (ShouldRetryUnhealthyIp(health))
                {
                    // 不健康的IP如果满足重试条件，也加入候选列表（但优先级较低）
                    unhealthyIps.Add(ip);
                }
            }
            else
            {
                // 未知状态的IP默认认为是健康的
                healthyIps.Add(ip);
            }
        }

        // 健康的IP优先，然后是可重试的不健康IP
        healthyIps.AddRange(unhealthyIps);
        return healthyIps;
    }

    /// <summary>
    /// 获取IP的健康信息
    /// </summary>
    public IpHealthInfo? GetHealthInfo(string ip)
    {
        return _ipHealth.TryGetValue(ip, out var health) ? health : null;
    }

    /// <summary>
    /// 判断不健康的IP是否应该重试
    /// </summary>
    private static bool ShouldRetryUnhealthyIp(IpHealthInfo health)
    {
        // 如果失败次数过多（超过5次），且最近5分钟内失败过，则不重试
        if (health.FailureCount > 5 && 
            health.LastFailureTime.HasValue && 
            DateTime.UtcNow - health.LastFailureTime.Value < TimeSpan.FromMinutes(5))
        {
            return false;
        }

        // 其他情况允许重试
        return true;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    private async void PerformHealthCheck(object? state)
    {
        var tasks = new List<Task>();

        foreach (var (ip, health) in _ipHealth)
        {
            // 只检查不健康的IP，看是否恢复
            if (!health.IsHealthy)
            {
                tasks.Add(CheckIpHealthAsync(ip, health));
            }
        }

        if (tasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _config.Logger?.LogError("Health check failed", ex);
            }
        }
    }

    /// <summary>
    /// 检查单个IP的健康状态
    /// </summary>
    private async Task CheckIpHealthAsync(string ip, IpHealthInfo health)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, 5000); // 5秒超时

            if (reply.Status == IPStatus.Success)
            {
                MarkHealthy(ip);
            }
        }
        catch (Exception ex)
        {
            _config.Logger?.LogDebug($"Ping check failed for IP {ip}: {ex.Message}");
            // ping失败不更新状态，等待实际连接测试
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
    }
}

/// <summary>
/// IP健康信息
/// </summary>
public class IpHealthInfo
{
    /// <summary>
    /// 是否健康
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// 失败次数
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 最后一次成功时间
    /// </summary>
    public DateTime? LastSuccessTime { get; set; }

    /// <summary>
    /// 最后一次失败时间
    /// </summary>
    public DateTime? LastFailureTime { get; set; }

    /// <summary>
    /// 最后一次异常
    /// </summary>
    public Exception? LastException { get; set; }
}