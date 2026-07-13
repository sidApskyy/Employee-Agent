using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Screenshot.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace RDCS.EmployeeAgent.Runtime.Workers;

public abstract class BackgroundWorkerBase : IBackgroundWorker, IHostedService, IDisposable
{
    protected readonly IAgentLogger Logger;
    protected readonly IEventBus EventBus;
    protected readonly CancellationTokenSource WorkerCts = new();
    
    private Task? _workerTask;
    private DateTime _startTime;
    private int _errorCount;
    private int _successCount;

    public abstract string Name { get; }
    public WorkerState State { get; protected set; }
    public WorkerHealth Health { get; protected set; } = new WorkerHealth
    {
        Status = HealthStatus.Unknown,
        LastCheckTimeUtc = DateTime.UtcNow
    };
    public WorkerConfiguration Configuration { get; set; } = new WorkerConfiguration();

    protected BackgroundWorkerBase(IAgentLogger logger, IEventBus eventBus)
    {
        Logger = logger;
        EventBus = eventBus;
        State = WorkerState.Stopped;
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ScreenshotWorkerTracer.Trace($"WORKER_FRAMEWORK: StartAsync called for {Name}");
        if (State == WorkerState.Running)
        {
            Logger.LogWarning(LogCategory.Application, "Worker {Name} is already running", Name);
            return;
        }

        State = WorkerState.Starting;
        _startTime = DateTime.UtcNow;
        _errorCount = 0;
        _successCount = 0;

        await OnStartingAsync(cancellationToken);

        // Link the worker's cancellation token with the provided one
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, WorkerCts.Token);

        // Initialize worker before starting the execution loop so configuration is complete
        await OnStartedAsync(cancellationToken);

        _workerTask = Task.Run(() => ExecuteWorkerLoopAsync(linkedCts.Token), linkedCts.Token);

        State = WorkerState.Running;
        UpdateHealth(HealthStatus.Healthy, "Worker started successfully");

        Logger.LogInformation(LogCategory.Application, "Worker {Name} started", Name);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (State == WorkerState.Stopped)
        {
            return;
        }

        State = WorkerState.Stopping;

        await OnStoppingAsync(cancellationToken);

        WorkerCts.Cancel();

        if (_workerTask != null)
        {
            try
            {
                await _workerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        State = WorkerState.Stopped;
        UpdateHealth(HealthStatus.Unknown, "Worker stopped");

        await OnStoppedAsync(cancellationToken);

        Logger.LogInformation(LogCategory.Application, "Worker {Name} stopped", Name);
    }

    public virtual async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (State != WorkerState.Running)
        {
            Logger.LogWarning(LogCategory.Application, "Worker {Name} pause requested but state is {State} — ignoring", Name, State);
            return;
        }

        State = WorkerState.Paused;
        UpdateHealth(HealthStatus.Degraded, "Worker paused");

        await OnPausedAsync(cancellationToken);

        Logger.LogInformation(LogCategory.Application, "Worker {Name} paused", Name);
    }

    public virtual async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (State != WorkerState.Paused)
        {
            Logger.LogWarning(LogCategory.Application, "Worker {Name} resume requested but state is {State} — ignoring", Name, State);
            return;
        }

        State = WorkerState.Running;
        UpdateHealth(HealthStatus.Healthy, "Worker resumed");

        await OnResumedAsync(cancellationToken);

        Logger.LogInformation(LogCategory.Application, "Worker {Name} resumed", Name);
    }

    public virtual async Task<WorkerHealth> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        Health.Uptime = DateTime.UtcNow - _startTime;
        Health.ErrorCount = _errorCount;
        Health.SuccessCount = _successCount;
        Health.LastCheckTimeUtc = DateTime.UtcNow;

        return Health;
    }

    protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
    protected abstract Task OnErrorAsync(Exception exception, CancellationToken cancellationToken);

    protected virtual Task OnStartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnStoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnPausedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnResumedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var lastException = new Exception();

        while (retryCount <= Configuration.MaxRetryCount)
        {
            try
            {
                await action();
                _successCount++;
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                _errorCount++;

                if (retryCount <= Configuration.MaxRetryCount)
                {
                    Logger.LogWarning(LogCategory.Application, 
                        "Worker {Name} execution failed (attempt {RetryCount}/{MaxRetryCount}), retrying in {RetryDelay}s", 
                        Name, retryCount, Configuration.MaxRetryCount, Configuration.RetryDelay.TotalSeconds);
                    
                    await Task.Delay(Configuration.RetryDelay, cancellationToken);
                }
            }
        }

        await OnErrorAsync(lastException, cancellationToken);
        throw lastException;
    }

    protected void UpdateHealth(HealthStatus status, string message)
    {
        Health.Status = status;
        Health.Message = message;
        Health.LastCheckTimeUtc = DateTime.UtcNow;
        Health.Uptime = DateTime.UtcNow - _startTime;
        Health.ErrorCount = _errorCount;
        Health.SuccessCount = _successCount;
    }

    public void Dispose()
    {
        WorkerCts.Dispose();
    }

    private async Task ExecuteWorkerLoopAsync(CancellationToken cancellationToken)
    {
        ScreenshotWorkerTracer.Trace($"WORKER_FRAMEWORK: ExecuteWorkerLoopAsync started for {Name}");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (State == WorkerState.Paused)
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                try
                {
                    ScreenshotWorkerTracer.Trace($"WORKER_FRAMEWORK: {Name} calling ExecuteAsync");
                    await ExecuteWithRetryAsync(() => ExecuteAsync(cancellationToken), cancellationToken);
                    ScreenshotWorkerTracer.Trace($"WORKER_FRAMEWORK: {Name} ExecuteAsync returned");
                }
                catch (Exception ex)
                {
                    ScreenshotWorkerTracer.Trace($"WORKER_FRAMEWORK: {Name} ExecuteAsync exception {ex.GetType().Name}: {ex.Message}");
                    State = WorkerState.Error;
                    UpdateHealth(HealthStatus.Unhealthy, $"Worker error: {ex.Message}");
                    Logger.LogError(LogCategory.Exception, "Worker {Name} error", ex, Name);
                    
                    await OnErrorAsync(ex, cancellationToken);
                    
                    // Don't continue if state is Error
                    if (State == WorkerState.Error)
                    {
                        break;
                    }
                }

                ScreenshotWorkerTracer.Trace($"WORKER_FRAMEWORK: {Name} waiting {Configuration.ExecutionInterval.TotalSeconds}s");
                await Task.Delay(Configuration.ExecutionInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            ScreenshotWorkerTracer.Trace($"WORKER_FRAMEWORK: {Name} loop cancelled");
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            ScreenshotWorkerTracer.Trace($"WORKER_FRAMEWORK: {Name} loop exception {ex.GetType().Name}: {ex.Message}");
            State = WorkerState.Error;
            UpdateHealth(HealthStatus.Unhealthy, $"Worker loop error: {ex.Message}");
            Logger.LogError(LogCategory.Exception, "Worker {Name} loop error", ex, Name);
        }
    }
}
