using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface IUploadQueueService
{
    Task EnqueueAsync(UploadJob job, CancellationToken cancellationToken = default);
    Task<List<UploadJob>> DequeueBatchAsync(int maxCount, CancellationToken cancellationToken = default);
    Task MarkUploadingAsync(string jobId, CancellationToken cancellationToken = default);
    Task MarkUploadedAsync(string jobId, string uploadId, string s3ObjectKey, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(string jobId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string jobId, string errorMessage, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task ResetStuckJobsAsync(CancellationToken cancellationToken = default);
}
