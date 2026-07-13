namespace RDCS.EmployeeAgent.Runtime.Upload.Events;

public record UploadCompleted(string JobId, string EmployeeId, string S3ObjectKey, long FileSize, long ElapsedMs, DateTime CompletedAtUtc);
