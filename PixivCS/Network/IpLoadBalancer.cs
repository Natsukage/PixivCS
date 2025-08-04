using System.Collections.Concurrent;

namespace PixivCS.Network;

/// <summary>
/// IP负载均衡器
/// </summary>
public class IpLoadBalancer
{
    private readonly ConcurrentDictionary<string, int> _roundRobinCounters = new();
    private readonly ConcurrentDictionary<string, WeightedIpInfo> _weightedIps = new();
    private readonly IpHealthMonitor _healthMonitor;
    private readonly ConnectionConfig _config;

    /// <summary>
    /// 初始化IP负载均衡器
    /// </summary>
    /// <param name="config">连接配置</param>
    /// <param name="healthMonitor">健康监控器</param>
    public IpLoadBalancer(ConnectionConfig config, IpHealthMonitor healthMonitor)
    {
        _config = config;
        _healthMonitor = healthMonitor;
        InitializeWeights();
    }

    /// <summary>
    /// 初始化IP权重
    /// </summary>
    private void InitializeWeights()
    {
        foreach (var (host, ips) in _config.StaticIpMapping)
        {
            foreach (var ip in ips)
            {
                _weightedIps[ip] = new WeightedIpInfo
                {
                    Ip = ip,
                    Weight = 100, // 初始权重
                    CurrentConnections = 0
                };
            }
        }
    }

    /// <summary>
    /// 选择最佳IP地址
    /// </summary>
    public string? SelectBestIp(string host, LoadBalanceStrategy strategy = LoadBalanceStrategy.HealthyFirst)
    {
        var availableIps = _healthMonitor.GetHealthyIps(host);
        if (availableIps.Count == 0)
        {
            _config.Logger?.LogWarning($"No healthy IPs available for host: {host}");
            return null;
        }

        return strategy switch
        {
            LoadBalanceStrategy.RoundRobin => SelectRoundRobin(host, availableIps),
            LoadBalanceStrategy.HealthyFirst => SelectHealthyFirst(availableIps),
            LoadBalanceStrategy.WeightedRoundRobin => SelectWeightedRoundRobin(availableIps),
            LoadBalanceStrategy.LeastConnections => SelectLeastConnections(availableIps),
            _ => SelectHealthyFirst(availableIps)
        };
    }

    /// <summary>
    /// 轮询选择
    /// </summary>
    private string? SelectRoundRobin(string host, List<string> availableIps)
    {
        if (availableIps.Count == 0) return null;

        _roundRobinCounters.TryAdd(host, 0);
        var index = _roundRobinCounters[host] % availableIps.Count;
        _roundRobinCounters[host] = (_roundRobinCounters[host] + 1) % availableIps.Count;
        
        return availableIps[index];
    }

    /// <summary>
    /// 健康优先选择
    /// </summary>
    private string? SelectHealthyFirst(List<string> availableIps)
    {
        if (availableIps.Count == 0) return null;

        // 按健康状态和成功率排序
        var sortedIps = availableIps
            .Select(ip => new
            {
                Ip = ip,
                Health = _healthMonitor.GetHealthInfo(ip),
                Weight = _weightedIps.TryGetValue(ip, out var w) ? w : null
            })
            .OrderByDescending(x => x.Health?.IsHealthy ?? true)
            .ThenBy(x => x.Health?.FailureCount ?? 0)
            .ThenByDescending(x => x.Weight?.Weight ?? 100)
            .ToList();

        return sortedIps.First().Ip;
    }

    /// <summary>
    /// 加权轮询选择
    /// </summary>
    private string? SelectWeightedRoundRobin(List<string> availableIps)
    {
        if (availableIps.Count == 0) return null;

        // 计算总权重
        var weightedIps = availableIps
            .Where(ip => _weightedIps.ContainsKey(ip))
            .Select(ip => _weightedIps[ip])
            .ToList();

        if (weightedIps.Count == 0) return availableIps.First();

        var totalWeight = weightedIps.Sum(w => w.Weight);
        if (totalWeight == 0) return availableIps.First();

        // 使用随机数选择
        var random = new Random().Next(totalWeight);
        var currentWeight = 0;

        foreach (var weightedIp in weightedIps)
        {
            currentWeight += weightedIp.Weight;
            if (random < currentWeight)
            {
                return weightedIp.Ip;
            }
        }

        return weightedIps.First().Ip;
    }

    /// <summary>
    /// 最少连接选择
    /// </summary>
    private string? SelectLeastConnections(List<string> availableIps)
    {
        if (availableIps.Count == 0) return null;

        var weightedIps = availableIps
            .Where(ip => _weightedIps.ContainsKey(ip))
            .Select(ip => _weightedIps[ip])
            .OrderBy(w => w.CurrentConnections)
            .ThenByDescending(w => w.Weight)
            .ToList();

        return weightedIps.FirstOrDefault()?.Ip ?? availableIps.First();
    }

    /// <summary>
    /// 增加连接计数
    /// </summary>
    public void IncrementConnection(string ip)
    {
        if (_weightedIps.TryGetValue(ip, out var info))
        {
            Interlocked.Increment(ref info._currentConnections);
        }
    }

    /// <summary>
    /// 减少连接计数
    /// </summary>
    public void DecrementConnection(string ip)
    {
        if (_weightedIps.TryGetValue(ip, out var info))
        {
            Interlocked.Decrement(ref info._currentConnections);
        }
    }

    /// <summary>
    /// 更新IP权重（基于性能表现）
    /// </summary>
    public void UpdateWeight(string ip, bool isSuccess, TimeSpan responseTime)
    {
        if (!_weightedIps.TryGetValue(ip, out var info)) return;

        if (isSuccess)
        {
            // 成功：权重轻微增加，响应时间快的增加更多
            var bonus = responseTime.TotalMilliseconds < 1000 ? 2 : 1;
            info.Weight = Math.Min(200, info.Weight + bonus);
        }
        else
        {
            // 失败：权重减少
            info.Weight = Math.Max(10, info.Weight - 10);
        }
        
        _config.Logger?.LogDebug($"Updated weight for IP {ip}: {info.Weight} (success: {isSuccess}, time: {responseTime.TotalMilliseconds}ms)");
    }

    /// <summary>
    /// 获取IP统计信息
    /// </summary>
    public Dictionary<string, object> GetIpStatistics()
    {
        var stats = new Dictionary<string, object>();
        
        foreach (var (host, ips) in _config.StaticIpMapping)
        {
            var hostStats = new List<object>();
            
            foreach (var ip in ips)
            {
                var health = _healthMonitor.GetHealthInfo(ip);
                var weight = _weightedIps.TryGetValue(ip, out var w) ? w : null;
                
                hostStats.Add(new
                {
                    Ip = ip,
                    IsHealthy = health?.IsHealthy ?? true,
                    FailureCount = health?.FailureCount ?? 0,
                    Weight = weight?.Weight ?? 100,
                    CurrentConnections = weight?.CurrentConnections ?? 0,
                    LastFailure = health?.LastFailureTime,
                    LastSuccess = health?.LastSuccessTime
                });
            }
            
            stats[host] = hostStats;
        }
        
        return stats;
    }
}

/// <summary>
/// 负载均衡策略
/// </summary>
public enum LoadBalanceStrategy
{
    /// <summary>
    /// 轮询
    /// </summary>
    RoundRobin,
    
    /// <summary>
    /// 健康优先
    /// </summary>
    HealthyFirst,
    
    /// <summary>
    /// 加权轮询
    /// </summary>
    WeightedRoundRobin,
    
    /// <summary>
    /// 最少连接
    /// </summary>
    LeastConnections
}

/// <summary>
/// 加权IP信息
/// </summary>
public class WeightedIpInfo
{
    /// <summary>
    /// IP地址
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// 权重
    /// </summary>
    public int Weight { get; set; } = 100;

    /// <summary>
    /// 当前连接数（私有字段用于 Interlocked 操作）
    /// </summary>
    public int _currentConnections;

    /// <summary>
    /// 当前连接数
    /// </summary>
    public int CurrentConnections
    {
        get => _currentConnections;
        set => _currentConnections = value;
    }
}