namespace RDCS.EmployeeAgent.Runtime.Workers;

public class WorkerConfiguration
{
    public TimeSpan ExecutionInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableHealthChecks { get; set; } = true;
}
