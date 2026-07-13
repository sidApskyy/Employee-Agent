using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface IUploadStatisticsService
{
    Task<UploadStatistics> GetAsync(CancellationToken cancellationToken = default);
    Task RecordUploadAsync(bool success, long bytesUploaded, long elapsedMs, CancellationToken cancellationToken = default);
}
