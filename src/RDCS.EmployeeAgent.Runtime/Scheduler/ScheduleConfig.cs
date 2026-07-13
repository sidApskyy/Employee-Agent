namespace RDCS.EmployeeAgent.Runtime.Scheduler;

public class ScheduleConfig
{
    public ScheduleType Type { get; set; }
    public string? CronExpression { get; set; }
    public TimeSpan? Interval { get; set; }
    public DateTime? RunAt { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
