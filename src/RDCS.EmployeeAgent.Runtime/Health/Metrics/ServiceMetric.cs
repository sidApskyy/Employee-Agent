namespace RDCS.EmployeeAgent.Runtime.Health.Metrics;

public class ServiceMetric : HealthMetric
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public TimeSpan Uptime { get; set; }
    public int ErrorCount { get; set; }
}
