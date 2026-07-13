namespace RDCS.EmployeeAgent.Runtime.Scheduler;

public class ScheduledJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ScheduleConfig Config { get; set; } = new();
    public Func<CancellationToken, Task> JobAction { get; set; } = null!;
    public DateTime? NextRunTimeUtc { get; set; }
    public DateTime? LastRunTimeUtc { get; set; }
    public int ExecutionCount { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
