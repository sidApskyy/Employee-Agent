using RDCS.EmployeeAgent.Runtime.Health.Metrics;

namespace RDCS.EmployeeAgent.Runtime.Health;

public interface IHealthMonitor
{
    Task<HealthStatus> GetOverallHealthAsync(CancellationToken cancellationToken = default);
    Task<CpuMetric> GetCpuMetricAsync(CancellationToken cancellationToken = default);
    Task<RamMetric> GetRamMetricAsync(CancellationToken cancellationToken = default);
    Task<DiskMetric> GetDiskMetricAsync(CancellationToken cancellationToken = default);
    Task<InternetMetric> GetInternetMetricAsync(CancellationToken cancellationToken = default);
    Task<QueueMetric> GetQueueMetricAsync(CancellationToken cancellationToken = default);
    Task<DatabaseMetric> GetDatabaseMetricAsync(CancellationToken cancellationToken = default);
    Task<ServiceMetric> GetServiceMetricAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<List<HealthMetric>> GetAllMetricsAsync(CancellationToken cancellationToken = default);
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);
}
