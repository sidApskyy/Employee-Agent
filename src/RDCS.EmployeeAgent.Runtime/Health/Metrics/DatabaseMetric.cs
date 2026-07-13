namespace RDCS.EmployeeAgent.Runtime.Health.Metrics;

public class DatabaseMetric : HealthMetric
{
    public bool IsConnected { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public TimeSpan? LastBackupAge { get; set; }
}
