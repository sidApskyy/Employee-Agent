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
        await _repository.NormalizeQueuePrioritiesAsync(cancellationToken);
    }

    /// <summary>
    /// Re-enqueue screenshots that were captured and saved to disk but never made it
    /// into UploadQueue (e.g. enqueue threw on the missing CaptureId column before the
    /// schema migration). Runs in the background after startup — it can scan tens of
    /// thousands of rows on old installs, so it must never block the worker loop.
    /// </summary>
    public async Task RecoverOrphanedScreenshotsAsync(CancellationToken cancellationToken)
    {
        List<UploadJob> orphans;
        try
        {
            orphans = await _repository.GetOrphanedScreenshotUploadsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Older DBs may lack Screenshots columns — never let recovery break startup
            _logger.LogWarning(LogCategory.Application,
                "UploadQueueService: orphan screenshot scan skipped - {Message}", ex.Message);
            return;
        }

        var recovered = 0;

        foreach (var job in orphans)
        {
            try
            {
                if (string.IsNullOrEmpty(job.LocalFilePath) || !System.IO.File.Exists(job.LocalFilePath))
                {
                    continue; // file gone — nothing to upload
                }

                job.JobId = Guid.NewGuid().ToString();
                job.Priority = 0;          // backlog — fresh captures (5) go first
                job.MaxRetryCount = 5;
                await EnqueueAsync(job, cancellationToken);
                recovered++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(LogCategory.Application,
                    "UploadQueueService: failed to recover orphaned screenshot {Path} - {Message}",
                    job.LocalFilePath, ex.Message);
            }
        }

        if (recovered > 0)
        {
            _logger.LogWarning(LogCategory.Application,
                "UploadQueueService: recovered {Count} orphaned screenshots into the upload queue", recovered);
        }
    }

    public async Task ExpediteAllRetriesAsync(CancellationToken cancellationToken = default)
    {
        await _repository.ExpediteAllRetriesAsync(cancellationToken);
    }
}
