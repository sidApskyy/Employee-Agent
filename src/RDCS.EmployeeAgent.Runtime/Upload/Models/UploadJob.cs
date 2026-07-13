namespace RDCS.EmployeeAgent.Runtime.Upload.Models;

public class UploadJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string LocalFilePath { get; set; } = string.Empty;
    public string? S3ObjectKey { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetryCount { get; set; } = 5;
    public int Priority { get; set; } = 5;
    public UploadStatus Status { get; set; } = UploadStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? NextRetryAtUtc { get; set; }
    public DateTime? UploadedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UploadId { get; set; }
}
