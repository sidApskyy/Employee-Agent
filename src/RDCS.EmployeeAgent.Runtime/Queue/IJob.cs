namespace RDCS.EmployeeAgent.Runtime.Queue;

public interface IJob
{
    string JobId { get; }
    string JobType { get; }
    JobPriority Priority { get; }
    JobState State { get; }
    DateTime CreatedAtUtc { get; }
    DateTime? ScheduledAtUtc { get; }
    int RetryCount { get; }
    string? Error { get; }
}
