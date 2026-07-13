namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageResponse
{
    public bool Success { get; set; }
    public string? Key { get; set; }
    public string? Url { get; set; }
    public long? SizeBytes { get; set; }
    public string? ETag { get; set; }
    public DateTime? UploadedAtUtc { get; set; }
    public string? Error { get; set; }
    public Stream? Content { get; set; }
}
