namespace RDCS.EmployeeAgent.Runtime.Screenshot.Models;

public class ScreenshotJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string MonitorId { get; set; } = string.Empty;
    public DateTime CaptureTimeUtc { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public bool Compressed { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetryCount { get; set; } = 3;
    public int Priority { get; set; } = 1;
    public string Status { get; set; } = "Pending";
    public string? Error { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
}
