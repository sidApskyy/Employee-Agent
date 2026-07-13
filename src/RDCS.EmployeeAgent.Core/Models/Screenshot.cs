namespace RDCS.EmployeeAgent.Core.Models;

public class Screenshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string MonitorId { get; set; } = string.Empty;
    public DateTime CaptureTimeUtc { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
    public int Quality { get; set; }
    public bool Compressed { get; set; }
    public long FileSizeBytes { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string UploadStatus { get; set; } = "Pending";
    public DateTime? UploadedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
