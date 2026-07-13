namespace RDCS.EmployeeAgent.Runtime.Upload.Events;

public record UploadVerified(string JobId, string Checksum, bool ChecksumMatched, DateTime VerifiedAtUtc);
