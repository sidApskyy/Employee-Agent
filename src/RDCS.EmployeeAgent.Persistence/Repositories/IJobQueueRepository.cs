namespace RDCS.EmployeeAgent.Persistence.Repositories;

public interface IJobQueueRepository
{
    Task<string> EnqueueAsync<T>(T job, int priority, CancellationToken cancellationToken = default);
    Task<JobQueueItem?> DequeueAsync(CancellationToken cancellationToken = default);
    Task UpdateJobStateAsync(string jobId, string newState, string? error = null, CancellationToken cancellationToken = default);
    Task<JobQueueItem?> GetJobAsync(string jobId, CancellationToken cancellationToken = default);
    Task<List<JobQueueItem>> GetPendingJobsAsync(int limit, CancellationToken cancellationToken = default);
    Task<List<JobQueueItem>> GetFailedJobsAsync(int limit, CancellationToken cancellationToken = default);
}

public class JobQueueItem
{
    public int Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public int JobPriority { get; set; }
    public string JobState { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
}
