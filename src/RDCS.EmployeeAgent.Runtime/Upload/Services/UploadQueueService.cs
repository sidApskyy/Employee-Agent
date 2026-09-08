using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Upload.Events;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Services;

public class UploadQueueService : IUploadQueueService
{
    private readonly IUploadRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly IAgentLogger _logger;

    public UploadQueueService(IUploadRepository repository, IEventBus eventBus, IAgentLogger logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task EnqueueAsync(UploadJob job, CancellationToken cancellationToken = default)
    {
        job.Status = UploadStatus.Pending;
        job.CreatedAtUtc = DateTime.UtcNow;

        await _repository.InsertJobAsync(job, cancellationToken);
        await _repository.RecordHistoryAsync(job.JobId, "Pending", "Job enqueued", cancellationToken);

        await _eventBus.PublishAsync(new UploadQueued(
            job.JobId, job.EmployeeId, job.DeviceId, job.LocalFilePath, DateTime.UtcNow), cancellationToken);

        _logger.LogInformation(LogCategory.Application, "Upload job enqueued: {JobId} File={File}", job.JobId, job.LocalFilePath);
    }

    public async Task<List<UploadJob>> DequeueBatchAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        // Single query fetches both Pending and due Retrying jobs, ordered by priority then age
        return await _repository.GetPendingJobsAsync(maxCount, cancellationToken);
    }

    public async Task MarkUploadingAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateStatusAsync(jobId, "Uploading", cancellationToken);
        await _repository.RecordHistoryAsync(jobId, "Uploading", null, cancellationToken);
    }

    public async Task MarkUploadedAsync(string jobId, string uploadId, string s3ObjectKey, CancellationToken cancellationToken = default)
    {
        await _repository.MarkUploadedAsync(jobId, uploadId, s3ObjectKey, cancellationToken);
        await _repository.RecordHistoryAsync(jobId, "Uploaded", $"S3Key={s3ObjectKey}", cancellationToken);
    }

    public async Task MarkCompletedAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _repository.MarkCompletedAsync(jobId, cancellationToken);
        await _repository.RecordHistoryAsync(jobId, "Completed", null, cancellationToken);
    }

    public async Task MarkFailedAsync(string jobId, string errorMessage, CancellationToken cancellationToken = default)
    {
        await _repository.MarkFailedAsync(jobId, errorMessage, cancellationToken);
        await _repository.RecordHistoryAsync(jobId, "Failed", errorMessage, cancellationToken);
        await _repository.RecordFailureAsync(jobId, errorMessage, null, cancellationToken);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
        => await _repository.GetPendingCountAsync(cancellationToken);

    public async Task ResetStuckJobsAsync(CancellationToken cancellationToken = default)
    {
        await _repository.ResetStuckUploadingJobsAsync(cancellationToken);
        _logger.LogWarning(LogCategory.Application,
            "UploadQueueService: Reset stuck Uploading/Preparing jobs to Pending after crash recovery");
    }

    public async Task ExpediteAllRetriesAsync(CancellationToken cancellationToken = default)
    {
        await _repository.ExpediteAllRetriesAsync(cancellationToken);
    }
}
