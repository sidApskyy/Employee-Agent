namespace RDCS.EmployeeAgent.Runtime.Upload.Events;

public record UploadStarted(string JobId, string EmployeeId, string DeviceId, long FileSize, DateTime StartedAtUtc);
