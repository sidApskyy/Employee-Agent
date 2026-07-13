namespace RDCS.EmployeeAgent.Runtime.Health.Metrics;

public class DiskMetric : HealthMetric
{
    public string DriveLetter { get; set; } = "C:";
    public long TotalBytes { get; set; }
    public long AvailableBytes { get; set; }
    public long UsedBytes { get; set; }
    public double UsedPercent { get; set; }
}
