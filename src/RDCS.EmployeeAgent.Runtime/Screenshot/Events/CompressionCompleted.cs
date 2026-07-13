namespace RDCS.EmployeeAgent.Runtime.Screenshot.Events;

public record CompressionCompleted(
    string ScreenshotId,
    long OriginalSize,
    long CompressedSize,
    double CompressionRatio,
    long Duration
);
