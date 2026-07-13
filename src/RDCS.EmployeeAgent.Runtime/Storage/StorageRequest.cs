namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageRequest
{
    public string Key { get; set; } = string.Empty;
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = "application/octet-stream";
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? BucketName { get; set; }
}
