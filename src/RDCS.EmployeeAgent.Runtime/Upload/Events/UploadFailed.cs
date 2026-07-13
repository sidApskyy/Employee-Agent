namespace RDCS.EmployeeAgent.Runtime.Upload.Events;

public record UploadFailed(string JobId, string EmployeeId, string ErrorMessage, int RetryCount, DateTime FailedAtUtc);
