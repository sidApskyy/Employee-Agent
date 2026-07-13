namespace RDCS.EmployeeAgent.Runtime.Health.Metrics;

public class InternetMetric : HealthMetric
{
    public bool IsConnected { get; set; }
    public string? IpAddress { get; set; }
    public double? LatencyMs { get; set; }
    public DateTime? LastConnectedUtc { get; set; }
}
