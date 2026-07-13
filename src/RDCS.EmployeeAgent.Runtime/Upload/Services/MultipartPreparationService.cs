using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

namespace RDCS.EmployeeAgent.Runtime.Upload.Services;

public class MultipartPreparationService : IMultipartPreparationService
{
    private const long DefaultPartSize = 10 * 1024 * 1024; // 10MB per part

    public bool RequiresMultipart(long fileSizeBytes, long thresholdBytes = 104857600)
        => fileSizeBytes >= thresholdBytes;

    public async Task<MultipartUploadPlan> PrepareAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(filePath);
        var totalSize = fileInfo.Length;
        var isMultipart = RequiresMultipart(totalSize);
        var totalParts = isMultipart ? (int)Math.Ceiling((double)totalSize / DefaultPartSize) : 1;

        return await Task.FromResult(new MultipartUploadPlan
        {
            FilePath = filePath,
            TotalSize = totalSize,
            TotalParts = totalParts,
            PartSize = DefaultPartSize,
            IsMultipart = isMultipart
        });
    }
}
