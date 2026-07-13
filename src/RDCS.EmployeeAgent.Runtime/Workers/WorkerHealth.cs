namespace RDCS.EmployeeAgent.Runtime.Workers;

public class WorkerHealth
{
    public HealthStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime LastCheckTimeUtc { get; set; }
    public TimeSpan Uptime { get; set; }
    public int ErrorCount { get; set; }
    public int SuccessCount { get; set; }
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
