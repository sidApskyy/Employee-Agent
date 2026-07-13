namespace RDCS.EmployeeAgent.Runtime.Screenshot.Events;

public record ScreenshotSaved(
    string ScreenshotId,
    string FilePath,
    string StoragePath,
    long FileSize,
    DateTime Timestamp
);
