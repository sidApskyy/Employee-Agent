namespace RDCS.EmployeeAgent.Runtime.Upload.Events;

public record UploadRetry(string JobId, string EmployeeId, int RetryCount, int MaxRetryCount, DateTime NextRetryAtUtc);
