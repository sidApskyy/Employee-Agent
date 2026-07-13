namespace RDCS.EmployeeAgent.Runtime.Health.Metrics;

public class CpuMetric : HealthMetric
{
    public double CpuPercent { get; set; }
    public int ProcessCount { get; set; }
}
