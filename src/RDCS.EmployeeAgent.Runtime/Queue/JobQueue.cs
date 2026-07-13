using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.EventBus.Events;
using System.Text.Json;

namespace RDCS.EmployeeAgent.Runtime.Queue;

public class JobQueue : IJobQueue
{
    private readonly IJobQueueRepository _repository;
    private readonly IAgentLogger _logger;
    private readonly IEventBus _eventBus;

    public JobQueue(IJobQueueRepository repository, IAgentLogger logger, IEventBus eventBus)
    {
        _repository = repository;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<string> EnqueueAsync<T>(T job, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default)
    {
        var jobId = await _repository.EnqueueAsync(job, (int)priority, cancellationToken);
        
        await _eventBus.PublishAsync(new JobEnqueued(jobId, typeof(T).Name, DateTime.UtcNow), cancellationToken);
        
        _logger.LogInformation(LogCategory.Application, "Job enqueued: {JobType} with ID {JobId}", typeof(T).Name, jobId);
        
        return jobId;
    }

    public async Task<IJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var jobItem = await _repository.DequeueAsync(cancellationToken);
        
        if (jobItem != null)
        {
            return new QueueJob
            {
                JobId = jobItem.Id.ToString(),
                JobType = jobItem.JobType,
                Priority = (JobPriority)jobItem.JobPriority,
                State = Enum.Parse<JobState>(jobItem.JobState),
                CreatedAtUtc = jobItem.CreatedAtUtc,
                ScheduledAtUtc = jobItem.ScheduledAtUtc,
                RetryCount = jobItem.RetryCount,
                Error = jobItem.Error
            };
        }
        
        return null;
    }

    public async Task UpdateJobStateAsync(string jobId, JobState newState, string? error = null, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateJobStateAsync(jobId, newState.ToString(), error, cancellationToken);
        
        if (newState == JobState.Completed)
        {
            await _eventBus.PublishAsync(new JobCompleted(jobId, "Success", DateTime.UtcNow), cancellationToken);
        }
        else if (newState == JobState.Failed)
        {
            await _eventBus.PublishAsync(new JobFailed(jobId, error ?? "Unknown error", DateTime.UtcNow), cancellationToken);
        }
    }

    public async Task<List<IJob>> GetPendingJobsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var jobItems = await _repository.GetPendingJobsAsync(limit, cancellationToken);
        return jobItems.Select(MapToJob).Cast<IJob>().ToList();
    }

    public async Task<List<IJob>> GetFailedJobsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var jobItems = await _repository.GetFailedJobsAsync(limit, cancellationToken);
        return jobItems.Select(MapToJob).Cast<IJob>().ToList();
    }

    public async Task<List<IJob>> GetDeadLetterJobsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        // For now, return failed jobs as dead letter jobs
        // In a full implementation, we'd have a separate dead letter table
        var jobItems = await _repository.GetFailedJobsAsync(limit, cancellationToken);
        return jobItems
            .Where(j => j.RetryCount >= j.MaxRetryCount)
            .Select(MapToJob)
            .Cast<IJob>()
            .ToList();
    }

    public async Task RetryJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateJobStateAsync(jobId, "Pending", null, cancellationToken);
        _logger.LogInformation(LogCategory.Application, "Job {JobId} marked for retry", jobId);
    }

    public async Task MoveToDeadLetterAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateJobStateAsync(jobId, "DeadLetter", "Max retries exceeded", cancellationToken);
        _logger.LogWarning(LogCategory.Application, "Job {JobId} moved to dead letter queue", jobId);
    }

    private IJob MapToJob(Persistence.Repositories.JobQueueItem item)
    {
        return new QueueJob
        {
            JobId = item.Id.ToString(),
            JobType = item.JobType,
            Priority = (JobPriority)item.JobPriority,
            State = Enum.Parse<JobState>(item.JobState),
            CreatedAtUtc = item.CreatedAtUtc,
            ScheduledAtUtc = item.ScheduledAtUtc,
            RetryCount = item.RetryCount,
            Error = item.Error
        };
    }

    private class QueueJob : IJob
    {
        public string JobId { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public JobPriority Priority { get; set; }
        public JobState State { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ScheduledAtUtc { get; set; }
        public int RetryCount { get; set; }
        public string? Error { get; set; }
    }
}
