using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Runtime.Scheduler;

public class Scheduler : IScheduler
{
    private readonly IAgentLogger _logger;
    private readonly Dictionary<string, ScheduledJob> _scheduledJobs = new();
    private readonly CancellationTokenSource _schedulerCts = new();
    private Task? _schedulerTask;
    private bool _isRunning;

    public Scheduler(IAgentLogger logger)
    {
        _logger = logger;
    }

    public string ScheduleCron(string name, string cronExpression, Func<CancellationToken, Task> job)
    {
        var config = new ScheduleConfig
        {
            Type = ScheduleType.Cron,
            CronExpression = cronExpression,
            Enabled = true
        };

        return ScheduleFromConfig(name, config, job);
    }

    public string ScheduleInterval(string name, TimeSpan interval, Func<CancellationToken, Task> job)
    {
        var config = new ScheduleConfig
        {
            Type = ScheduleType.Interval,
            Interval = interval,
            Enabled = true
        };

        return ScheduleFromConfig(name, config, job);
    }

    public string ScheduleOneTime(string name, DateTime runAt, Func<CancellationToken, Task> job)
    {
        var config = new ScheduleConfig
        {
            Type = ScheduleType.OneTime,
            RunAt = runAt,
            Enabled = true
        };

        return ScheduleFromConfig(name, config, job);
    }

    public string ScheduleFromConfig(string name, ScheduleConfig config, Func<CancellationToken, Task> job)
    {
        var scheduledJob = new ScheduledJob
        {
            Name = name,
            Config = config,
            JobAction = job,
            IsEnabled = config.Enabled,
            NextRunTimeUtc = CalculateNextRunTime(config)
        };

        _scheduledJobs[scheduledJob.JobId] = scheduledJob;

        _logger.LogInformation(LogCategory.Application, 
            "Scheduled job {Name} ({JobId}) with type {Type}, next run at {NextRunTimeUtc}", 
            name, scheduledJob.JobId, config.Type, scheduledJob.NextRunTimeUtc);

        return scheduledJob.JobId;
    }

    public void UpdateSchedule(string jobId, ScheduleConfig newConfig)
    {
        if (!_scheduledJobs.TryGetValue(jobId, out var job))
        {
            throw new ArgumentException($"Job {jobId} not found");
        }

        job.Config = newConfig;
        job.IsEnabled = newConfig.Enabled;
        job.NextRunTimeUtc = CalculateNextRunTime(newConfig);

        _logger.LogInformation(LogCategory.Application, 
            "Updated job {JobId} with new config, next run at {NextRunTimeUtc}", 
            jobId, job.NextRunTimeUtc);
    }

    public void CancelSchedule(string jobId)
    {
        if (_scheduledJobs.Remove(jobId, out var job))
        {
            _logger.LogInformation(LogCategory.Application, "Cancelled job {JobId}", jobId);
        }
    }

    public IReadOnlyList<ScheduledJob> GetScheduledJobs()
    {
        return _scheduledJobs.Values.ToList().AsReadOnly();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _logger.LogInformation(LogCategory.Application, "Scheduler started");

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _schedulerCts.Token);

        _schedulerTask = Task.Run(() => ExecuteSchedulerLoopAsync(linkedCts.Token), linkedCts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _schedulerCts.Cancel();

        if (_schedulerTask != null)
        {
            try
            {
                await _schedulerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        _logger.LogInformation(LogCategory.Application, "Scheduler stopped");
    }

    private async Task ExecuteSchedulerLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var jobsToRun = _scheduledJobs.Values
                    .Where(j => j.IsEnabled && j.NextRunTimeUtc.HasValue && j.NextRunTimeUtc <= now)
                    .ToList();

                foreach (var job in jobsToRun)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    _ = Task.Run(async () => await ExecuteJobAsync(job, cancellationToken), cancellationToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Scheduler loop error", ex);
        }
    }

    private async Task ExecuteJobAsync(ScheduledJob job, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(LogCategory.Application, 
                "Executing job {Name} ({JobId})", job.Name, job.JobId);

            await job.JobAction(cancellationToken);

            job.LastRunTimeUtc = DateTime.UtcNow;
            job.ExecutionCount++;

            // Update next run time based on schedule type
            if (job.Config.Type == ScheduleType.OneTime)
            {
                job.IsEnabled = false;
                _scheduledJobs.Remove(job.JobId);
                _logger.LogInformation(LogCategory.Application, 
                    "One-time job {Name} ({JobId}) completed and removed", job.Name, job.JobId);
            }
            else
            {
                job.NextRunTimeUtc = CalculateNextRunTime(job.Config);
                _logger.LogInformation(LogCategory.Application, 
                    "Job {Name} ({JobId}) completed, next run at {NextRunTimeUtc}", 
                    job.Name, job.JobId, job.NextRunTimeUtc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, 
                "Job {Name} ({JobId}) execution failed", ex, job.Name, job.JobId);
        }
    }

    private DateTime? CalculateNextRunTime(ScheduleConfig config)
    {
        return config.Type switch
        {
            ScheduleType.Interval when config.Interval.HasValue => DateTime.UtcNow.Add(config.Interval.Value),
            ScheduleType.OneTime when config.RunAt.HasValue => config.RunAt.Value,
            ScheduleType.Cron => CalculateCronNextRun(config.CronExpression),
            _ => null
        };
    }

    private DateTime CalculateCronNextRun(string? cronExpression)
    {
        // Simplified cron parsing - in production, use a proper cron library like NCrontab
        // For now, default to 1 minute from now
        return DateTime.UtcNow.AddMinutes(1);
    }
}
