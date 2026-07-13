namespace RDCS.EmployeeAgent.Runtime.Screenshot.Models;

public class CompressionStats
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ScreenshotId { get; set; } = string.Empty;
    public long OriginalSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }
    public double CompressionRatio { get; set; }
    public long CompressionDurationMs { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
