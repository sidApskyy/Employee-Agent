using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.EventBus.Events;
using RDCS.EmployeeAgent.Runtime.Health.Metrics;
using System.Diagnostics;

namespace RDCS.EmployeeAgent.Runtime.Health;

public class HealthMonitor : IHealthMonitor
{
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    private readonly IAgentLogger Logger;
    private readonly IEventBus _eventBus;
    private readonly CancellationTokenSource _monitorCts = new();
    private Task? _monitorTask;
    private bool _isMonitoring;
    private DateTime? _lastConnectedUtc;

    public HealthMonitor(IAgentLogger logger, IEventBus eventBus)
    {
        Logger = logger;
        _eventBus = eventBus;
    }

    public async Task<HealthStatus> GetOverallHealthAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await GetAllMetricsAsync(cancellationToken);
        
        if (metrics.Any(m => m.Status == HealthStatus.Unhealthy))
        {
            return HealthStatus.Unhealthy;
        }
        
        if (metrics.Any(m => m.Status == HealthStatus.Degraded))
        {
            return HealthStatus.Degraded;
        }
        
        return HealthStatus.Healthy;
    }

    public async Task<CpuMetric> GetCpuMetricAsync(CancellationToken cancellationToken = default)
    {
        var metric = new CpuMetric
        {
            Name = "CPU",
            MeasuredAtUtc = DateTime.UtcNow
        };

        try
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var cpuPercent = cpuCounter.NextValue();
            await Task.Delay(1000, cancellationToken); // Wait for counter to update
            cpuPercent = cpuCounter.NextValue();
            
            metric.CpuPercent = cpuPercent;
            metric.ProcessCount = Process.GetProcesses().Length;
            
            metric.Status = cpuPercent > 90 ? HealthStatus.Unhealthy : 
                           cpuPercent > 70 ? HealthStatus.Degraded : 
                           HealthStatus.Healthy;
            metric.Message = $"CPU usage: {cpuPercent:F1}%";
            
            metric.Data["CpuPercent"] = cpuPercent;
            metric.Data["ProcessCount"] = metric.ProcessCount;
        }
        catch (Exception ex)
        {
            metric.Status = HealthStatus.Unknown;
            metric.Message = $"Failed to get CPU metrics: {ex.Message}";
            Logger.LogError(LogCategory.Exception, "Failed to get CPU metrics", ex);
        }

        return metric;
    }

    public async Task<RamMetric> GetRamMetricAsync(CancellationToken cancellationToken = default)
    {
        var metric = new RamMetric
        {
            Name = "RAM",
            MeasuredAtUtc = DateTime.UtcNow
        };

        try
        {
            var memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            var availableMb = memoryCounter.NextValue();
            var totalMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);
            var usedMb = totalMb - availableMb;

            metric.TotalBytes = totalMb * 1024 * 1024;
            metric.AvailableBytes = (long)(availableMb * 1024 * 1024);
            metric.UsedBytes = (long)(usedMb * 1024 * 1024);
            metric.UsedPercent = (usedMb / totalMb) * 100;
            
            metric.Status = metric.UsedPercent > 90 ? HealthStatus.Unhealthy : 
                           metric.UsedPercent > 80 ? HealthStatus.Degraded : 
                           HealthStatus.Healthy;
            metric.Message = $"RAM usage: {metric.UsedPercent:F1}% ({usedMb:F0} MB / {totalMb:F0} MB)";
            
            metric.Data["TotalBytes"] = metric.TotalBytes;
            metric.Data["AvailableBytes"] = metric.AvailableBytes;
            metric.Data["UsedBytes"] = metric.UsedBytes;
            metric.Data["UsedPercent"] = metric.UsedPercent;
        }
        catch (Exception ex)
        {
            metric.Status = HealthStatus.Unknown;
            metric.Message = $"Failed to get RAM metrics: {ex.Message}";
            Logger.LogError(LogCategory.Exception, "Failed to get RAM metrics", ex);
        }

        return metric;
    }

    public async Task<DiskMetric> GetDiskMetricAsync(CancellationToken cancellationToken = default)
    {
        var metric = new DiskMetric
        {
            Name = "Disk",
            DriveLetter = "C:",
            MeasuredAtUtc = DateTime.UtcNow
        };

        try
        {
            var driveInfo = new DriveInfo("C:");
            
            metric.TotalBytes = driveInfo.TotalSize;
            metric.AvailableBytes = driveInfo.AvailableFreeSpace;
            metric.UsedBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
            metric.UsedPercent = (metric.UsedBytes / (double)metric.TotalBytes) * 100;
            
            metric.Status = metric.UsedPercent > 95 ? HealthStatus.Unhealthy : 
                           metric.UsedPercent > 85 ? HealthStatus.Degraded : 
                           HealthStatus.Healthy;
            metric.Message = $"Disk usage: {metric.UsedPercent:F1}% ({metric.UsedBytes / (1024 * 1024 * 1024):F0} GB / {metric.TotalBytes / (1024 * 1024 * 1024):F0} GB)";
            
            metric.Data["DriveLetter"] = metric.DriveLetter;
            metric.Data["TotalBytes"] = metric.TotalBytes;
            metric.Data["AvailableBytes"] = metric.AvailableBytes;
            metric.Data["UsedBytes"] = metric.UsedBytes;
            metric.Data["UsedPercent"] = metric.UsedPercent;
        }
        catch (Exception ex)
        {
            metric.Status = HealthStatus.Unknown;
            metric.Message = $"Failed to get disk metrics: {ex.Message}";
            Logger.LogError(LogCategory.Exception, "Failed to get disk metrics", ex);
        }

        return metric;
    }

    public async Task<InternetMetric> GetInternetMetricAsync(CancellationToken cancellationToken = default)
    {
        var metric = new InternetMetric
        {
            Name = "Internet",
            MeasuredAtUtc = DateTime.UtcNow
        };

        try
        {
            // Simple connectivity check
            var stopwatch = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync("https://www.google.com", cancellationToken);
            stopwatch.Stop();
            
            metric.IsConnected = response.IsSuccessStatusCode;
            metric.LatencyMs = stopwatch.ElapsedMilliseconds;
            
            if (metric.IsConnected)
            {
                _lastConnectedUtc = DateTime.UtcNow;
                metric.LastConnectedUtc = _lastConnectedUtc;
                metric.Status = HealthStatus.Healthy;
                metric.Message = $"Internet connected, latency: {metric.LatencyMs}ms";
            }
            else
            {
                metric.Status = HealthStatus.Unhealthy;
                metric.Message = "Internet disconnected";
                
                await _eventBus.PublishAsync(new InternetDisconnected(DateTime.UtcNow), cancellationToken);
            }
            
            metric.Data["IsConnected"] = metric.IsConnected;
            metric.Data["LatencyMs"] = metric.LatencyMs;
        }
        catch (Exception ex)
        {
            metric.IsConnected = false;
            metric.Status = HealthStatus.Unhealthy;
            metric.Message = $"Internet connection failed: {ex.Message}";
            
            await _eventBus.PublishAsync(new InternetDisconnected(DateTime.UtcNow), cancellationToken);
            
            Logger.LogError(LogCategory.Exception, "Failed to check internet connectivity", ex);
        }

        return metric;
    }

    public async Task<QueueMetric> GetQueueMetricAsync(CancellationToken cancellationToken = default)
    {
        var metric = new QueueMetric
        {
            Name = "Queue",
            MeasuredAtUtc = DateTime.UtcNow
        };

        try
        {
            // This would query the actual queue system
            // For now, return mock data
            metric.PendingCount = 0;
            metric.RunningCount = 0;
            metric.FailedCount = 0;
            metric.DeadLetterCount = 0;
            
            metric.Status = HealthStatus.Healthy;
            metric.Message = $"Queue status: {metric.PendingCount} pending, {metric.RunningCount} running, {metric.FailedCount} failed";
            
            metric.Data["PendingCount"] = metric.PendingCount;
            metric.Data["RunningCount"] = metric.RunningCount;
            metric.Data["FailedCount"] = metric.FailedCount;
            metric.Data["DeadLetterCount"] = metric.DeadLetterCount;
        }
        catch (Exception ex)
        {
            metric.Status = HealthStatus.Unknown;
            metric.Message = $"Failed to get queue metrics: {ex.Message}";
            Logger.LogError(LogCategory.Exception, "Failed to get queue metrics", ex);
        }

        return metric;
    }

    public async Task<DatabaseMetric> GetDatabaseMetricAsync(CancellationToken cancellationToken = default)
    {
        var metric = new DatabaseMetric
        {
            Name = "Database",
            MeasuredAtUtc = DateTime.UtcNow
        };

        try
        {
            // This would check actual SQLite database connection
            // For now, assume healthy
            metric.IsConnected = true;
            metric.DatabaseSizeBytes = 0;
            metric.LastBackupAge = null;
            
            metric.Status = HealthStatus.Healthy;
            metric.Message = "Database connected";
            
            metric.Data["IsConnected"] = metric.IsConnected;
            metric.Data["DatabaseSizeBytes"] = metric.DatabaseSizeBytes;
        }
        catch (Exception ex)
        {
            metric.IsConnected = false;
            metric.Status = HealthStatus.Unhealthy;
            metric.Message = $"Database connection failed: {ex.Message}";
            Logger.LogError(LogCategory.Exception, "Failed to check database health", ex);
        }

        return metric;
    }

    public async Task<ServiceMetric> GetServiceMetricAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var metric = new ServiceMetric
        {
            Name = $"Service-{serviceName}",
            ServiceName = serviceName,
            MeasuredAtUtc = DateTime.UtcNow
        };

        try
        {
            // This would check actual service status
            // For now, return mock data
            metric.IsRunning = true;
            metric.Uptime = TimeSpan.FromHours(1);
            metric.ErrorCount = 0;
            
            metric.Status = HealthStatus.Healthy;
            metric.Message = $"Service {serviceName} is running";
            
            metric.Data["ServiceName"] = metric.ServiceName;
            metric.Data["IsRunning"] = metric.IsRunning;
            metric.Data["Uptime"] = metric.Uptime;
            metric.Data["ErrorCount"] = metric.ErrorCount;
        }
        catch (Exception ex)
        {
            metric.Status = HealthStatus.Unknown;
            metric.Message = $"Failed to get service metrics: {ex.Message}";
            Logger.LogError(LogCategory.Exception, "Failed to get service metrics", ex);
        }

        return metric;
    }

    public async Task<List<HealthMetric>> GetAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new List<HealthMetric>();
        
        metrics.Add(await GetCpuMetricAsync(cancellationToken));
        metrics.Add(await GetRamMetricAsync(cancellationToken));
        metrics.Add(await GetDiskMetricAsync(cancellationToken));
        metrics.Add(await GetInternetMetricAsync(cancellationToken));
        metrics.Add(await GetQueueMetricAsync(cancellationToken));
        metrics.Add(await GetDatabaseMetricAsync(cancellationToken));
        
        return metrics;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_isMonitoring)
        {
            return;
        }

        _isMonitoring = true;
        Logger.LogInformation(LogCategory.Application, "Health Monitor started");

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _monitorCts.Token);

        _monitorTask = Task.Run(() => ExecuteMonitorLoopAsync(linkedCts.Token), linkedCts.Token);
    }

    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (!_isMonitoring)
        {
            return;
        }

        _isMonitoring = false;
        _monitorCts.Cancel();

        if (_monitorTask != null)
        {
            try
            {
                await _monitorTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        Logger.LogInformation(LogCategory.Application, "Health Monitor stopped");
    }

    private async Task ExecuteMonitorLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var health = await GetOverallHealthAsync(cancellationToken);
                
                if (health == HealthStatus.Unhealthy)
                {
                    Logger.LogWarning(LogCategory.Application, "System health is unhealthy");
                    await _eventBus.PublishAsync(new SystemUnhealthy("System health is unhealthy", DateTime.UtcNow), cancellationToken);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Health Monitor loop error", ex);
        }
    }
}
