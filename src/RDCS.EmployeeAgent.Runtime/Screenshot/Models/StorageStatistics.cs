namespace RDCS.EmployeeAgent.Runtime.Screenshot.Models;

public class StorageStatistics
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int TotalScreenshots { get; set; }
    public long TotalSizeBytes { get; set; }
    public DateTime? OldestScreenshotDate { get; set; }
    public DateTime? NewestScreenshotDate { get; set; }
    public double AverageSizeBytes { get; set; }
    public DateTime? LastCleanupTimeUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
