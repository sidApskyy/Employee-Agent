namespace RDCS.EmployeeAgent.Runtime.Screenshot.Events;

public record StorageCompleted(
    string ScreenshotId,
    string StoragePath,
    long FileSize,
    DateTime Timestamp
);
