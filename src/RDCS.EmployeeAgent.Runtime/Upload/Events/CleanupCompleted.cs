namespace RDCS.EmployeeAgent.Runtime.Upload.Events;

public record CleanupCompleted(string JobId, string LocalFilePath, bool Deleted, DateTime CompletedAtUtc);
