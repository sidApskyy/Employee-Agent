namespace RDCS.EmployeeAgent.Runtime.Health.Metrics;

public class QueueMetric : HealthMetric
{
    public int PendingCount { get; set; }
    public int RunningCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
}
