using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Upload.Events;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Services;

public class UploadRetryService : IUploadRetryService
{
    private readonly IUploadRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly IAgentLogger _logger;

    public UploadRetryService(IUploadRepository repository, IEventBus eventBus, IAgentLogger logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<bool> ShouldRetryAsync(UploadJob job, CancellationToken cancellationToken = default)
        => await Task.FromResult(job.RetryCount < job.MaxRetryCount);

    public async Task ScheduleRetryAsync(UploadJob job, string errorMessage, CancellationToken cancellationToken = default)
    {
        if (!await ShouldRetryAsync(job, cancellationToken))
        {
            _logger.LogWarning(LogCategory.Application,
                "Upload job {JobId} exceeded max retries ({Max}), moving to DeadLetter", job.JobId, job.MaxRetryCount);

            await _repository.MoveToDeadLetterAsync(job, errorMessage, cancellationToken);
            await _eventBus.PublishAsync(new UploadFailed(
                job.JobId, job.EmployeeId, errorMessage, job.RetryCount, DateTime.UtcNow), cancellationToken);
            return;
        }

        job.RetryCount++;
        job.Status = UploadStatus.Retrying;
        job.ErrorMessage = errorMessage;

        var delay = CalculateDelay(job.RetryCount, 1);
        job.NextRetryAtUtc = DateTime.UtcNow.Add(delay);

        await _repository.UpdateJobAsync(job, cancellationToken);
        await _repository.RecordHistoryAsync(job.JobId, "Retrying",
            $"Attempt {job.RetryCount}, next retry at {job.NextRetryAtUtc:O}", cancellationToken);

        await _eventBus.PublishAsync(new UploadRetry(
            job.JobId, job.EmployeeId, job.RetryCount, job.MaxRetryCount, job.NextRetryAtUtc.Value), cancellationToken);

        _logger.LogInformation(LogCategory.Application,
            "Upload job {JobId} scheduled for retry {Attempt}/{Max} at {NextRetry}",
            job.JobId, job.RetryCount, job.MaxRetryCount, job.NextRetryAtUtc);
    }

    public async Task ProcessDueRetriesAsync(CancellationToken cancellationToken = default)
    {
        var dueJobs = await _repository.GetRetryReadyJobsAsync(cancellationToken);
        foreach (var job in dueJobs)
        {
            job.Status = UploadStatus.Pending;
            job.NextRetryAtUtc = null;
            await _repository.UpdateJobAsync(job, cancellationToken);
            _logger.LogInformation(LogCategory.Application, "Upload job {JobId} moved back to Pending for retry", job.JobId);
        }
    }

    public TimeSpan CalculateDelay(int retryCount, int baseDelayMinutes)
    {
        var minutes = baseDelayMinutes * Math.Pow(2, retryCount - 1);
        return TimeSpan.FromMinutes(Math.Min(minutes, 60));
    }
}
