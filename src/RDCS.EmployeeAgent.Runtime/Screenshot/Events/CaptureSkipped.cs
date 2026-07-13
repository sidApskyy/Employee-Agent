namespace RDCS.EmployeeAgent.Runtime.Screenshot.Events;

public record CaptureSkipped(
    string EmployeeId,
    string DeviceId,
    string Reason,
    DateTime Timestamp
);
