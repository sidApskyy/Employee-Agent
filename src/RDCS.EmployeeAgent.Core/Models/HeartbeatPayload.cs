namespace RDCS.EmployeeAgent.Core.Models;

public class HeartbeatPayload
{
    public string EmployeeId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "1.0.0";
    public string ComputerName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = true;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ConfigVersion { get; set; } = string.Empty;
    public SystemMetrics? SystemMetrics { get; set; }
}

public class SystemMetrics
{
    public double CpuPercent { get; set; }
    public int MemoryUsedMb { get; set; }
}
