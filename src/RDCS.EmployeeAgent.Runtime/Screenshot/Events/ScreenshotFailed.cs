namespace RDCS.EmployeeAgent.Runtime.Screenshot.Events;

public record ScreenshotFailed(
    string ScreenshotId,
    string EmployeeId,
    string DeviceId,
    string Error,
    DateTime Timestamp
);
