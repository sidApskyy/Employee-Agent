namespace RDCS.EmployeeAgent.Runtime.Upload.Events;

public record UploadQueued(string JobId, string EmployeeId, string DeviceId, string LocalFilePath, DateTime QueuedAtUtc);
