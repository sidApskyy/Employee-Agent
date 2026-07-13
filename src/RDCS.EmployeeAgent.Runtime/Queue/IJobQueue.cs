namespace RDCS.EmployeeAgent.Runtime.Queue;

public interface IJobQueue
{
    Task<string> EnqueueAsync<T>(T job, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);
    Task<IJob?> DequeueAsync(CancellationToken cancellationToken = default);
    Task UpdateJobStateAsync(string jobId, JobState newState, string? error = null, CancellationToken cancellationToken = default);
    Task<List<IJob>> GetPendingJobsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<List<IJob>> GetFailedJobsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<List<IJob>> GetDeadLetterJobsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task RetryJobAsync(string jobId, CancellationToken cancellationToken = default);
    Task MoveToDeadLetterAsync(string jobId, CancellationToken cancellationToken = default);
}
