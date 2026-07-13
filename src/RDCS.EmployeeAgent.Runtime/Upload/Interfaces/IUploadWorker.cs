using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface IUploadWorker
{
    Task EnqueueUploadAsync(UploadJob job, CancellationToken cancellationToken = default);
    Task<UploadStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
