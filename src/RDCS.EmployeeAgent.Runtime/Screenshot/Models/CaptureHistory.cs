namespace RDCS.EmployeeAgent.Runtime.Screenshot.Models;

public class CaptureHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DateTime CaptureTimeUtc { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public long DurationMs { get; set; }
    public int MonitorCount { get; set; }
    public long TotalSizeBytes { get; set; }
}
