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
        var pending = await _repository.GetPendingJobsAsync(maxCount, cancellationToken);
        var retryReady = await _repository.GetRetryReadyJobsAsync(cancellationToken);

        var batch = pending.Concat(retryReady)
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.CreatedAtUtc)
            .Take(maxCount)
            .ToList();

        return batch;
    }

    public async Task MarkUploadingAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetJobByIdAsync(jobId, cancellationToken);
        if (job == null) return;

        job.Status = UploadStatus.Uploading;
        await _repository.UpdateJobAsync(job, cancellationToken);
        await _repository.RecordHistoryAsync(jobId, "Uploading", null, cancellationToken);
    }

    public async Task MarkUploadedAsync(string jobId, string uploadId, string s3ObjectKey, CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetJobByIdAsync(jobId, cancellationToken);
        if (job == null) return;

        job.Status = UploadStatus.Uploaded;
        job.UploadId = uploadId;
        job.S3ObjectKey = s3ObjectKey;
        job.UploadedAtUtc = DateTime.UtcNow;

        await _repository.UpdateJobAsync(job, cancellationToken);
        await _repository.RecordHistoryAsync(jobId, "Uploaded", $"S3Key={s3ObjectKey}", cancellationToken);
    }

    public async Task MarkCompletedAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetJobByIdAsync(jobId, cancellationToken);
        if (job == null) return;

        job.Status = UploadStatus.Completed;
        job.CompletedAtUtc = DateTime.UtcNow;

        await _repository.UpdateJobAsync(job, cancellationToken);
        await _repository.RecordHistoryAsync(jobId, "Completed", null, cancellationToken);
    }

    public async Task MarkFailedAsync(string jobId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetJobByIdAsync(jobId, cancellationToken);
        if (job == null) return;

        job.Status = UploadStatus.Failed;
        job.ErrorMessage = errorMessage;

        await _repository.UpdateJobAsync(job, cancellationToken);
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
}
