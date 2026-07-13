namespace RDCS.EmployeeAgent.Runtime.Screenshot.Events;

public record ScreenshotCaptured(
    string ScreenshotId,
    string EmployeeId,
    string DeviceId,
    string MonitorId,
    DateTime CaptureTime,
    string FilePath,
    long FileSize
);
