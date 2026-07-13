namespace RDCS.EmployeeAgent.Runtime.Upload.Models;

public class UploadStatistics
{
    public int PendingCount { get; set; }
    public int UploadingCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public int RetryingCount { get; set; }
    public int DeadLetterCount { get; set; }
    public long TotalBytesUploaded { get; set; }
    public double AverageUploadMs { get; set; }
    public double UploadSpeedBytesPerSec { get; set; }
    public double SuccessRate { get; set; }
    public double FailureRate { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
