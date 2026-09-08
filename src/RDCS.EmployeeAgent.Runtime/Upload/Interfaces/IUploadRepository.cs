using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface IUploadRepository
{
    Task InsertJobAsync(UploadJob job, CancellationToken cancellationToken = default);
    Task UpdateJobAsync(UploadJob job, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string jobId, string status, CancellationToken cancellationToken = default);
    Task MarkUploadedAsync(string jobId, string uploadId, string s3ObjectKey, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(string jobId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string jobId, string errorMessage, CancellationToken cancellationToken = default);
    Task<UploadJob?> GetJobByIdAsync(string jobId, CancellationToken cancellationToken = default);
    Task<List<UploadJob>> GetPendingJobsAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<List<UploadJob>> GetRetryReadyJobsAsync(CancellationToken cancellationToken = default);
    Task<List<UploadJob>> GetDeadLetterJobsAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task RecordHistoryAsync(string jobId, string status, string? message, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(string jobId, string errorMessage, string? stackTrace, CancellationToken cancellationToken = default);
    Task MoveToDeadLetterAsync(UploadJob job, string reason, CancellationToken cancellationToken = default);
    Task UpdateDailyStatisticsAsync(bool success, long bytesUploaded, long elapsedMs, CancellationToken cancellationToken = default);
    Task<UploadStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task ResetStuckUploadingJobsAsync(CancellationToken cancellationToken = default);
    Task ExpediteAllRetriesAsync(CancellationToken cancellationToken = default);
}
