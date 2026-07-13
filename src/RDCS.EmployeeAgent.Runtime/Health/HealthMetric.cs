namespace RDCS.EmployeeAgent.Runtime.Health;

public class HealthMetric
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime MeasuredAtUtc { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}
