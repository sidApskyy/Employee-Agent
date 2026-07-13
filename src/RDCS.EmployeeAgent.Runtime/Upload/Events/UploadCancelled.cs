namespace RDCS.EmployeeAgent.Runtime.Upload.Events;

public record UploadCancelled(string JobId, string EmployeeId, string Reason, DateTime CancelledAtUtc);
