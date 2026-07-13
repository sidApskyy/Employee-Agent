namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface IMultipartPreparationService
{
    bool RequiresMultipart(long fileSizeBytes, long thresholdBytes = 104857600);
    Task<MultipartUploadPlan> PrepareAsync(string filePath, CancellationToken cancellationToken = default);
}

public class MultipartUploadPlan
{
    public string FilePath { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public int TotalParts { get; set; }
    public long PartSize { get; set; }
    public bool IsMultipart { get; set; }
}
