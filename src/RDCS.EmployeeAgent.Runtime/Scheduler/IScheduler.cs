namespace RDCS.EmployeeAgent.Runtime.Scheduler;

public interface IScheduler
{
    string ScheduleCron(string name, string cronExpression, Func<CancellationToken, Task> job);
    string ScheduleInterval(string name, TimeSpan interval, Func<CancellationToken, Task> job);
    string ScheduleOneTime(string name, DateTime runAt, Func<CancellationToken, Task> job);
    string ScheduleFromConfig(string name, ScheduleConfig config, Func<CancellationToken, Task> job);
    void UpdateSchedule(string jobId, ScheduleConfig newConfig);
    void CancelSchedule(string jobId);
    IReadOnlyList<ScheduledJob> GetScheduledJobs();
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
