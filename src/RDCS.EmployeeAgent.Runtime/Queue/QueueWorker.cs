using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.EventBus.Events;

namespace RDCS.EmployeeAgent.Runtime.Queue;

public class QueueWorker : IQueueWorker
{
    private readonly IJobQueue _jobQueue;
    private readonly IJobProcessor _jobProcessor;
    private readonly IAgentLogger _logger;
    private readonly IEventBus _eventBus;
    private readonly CancellationTokenSource _workerCts = new();
    private Task? _workerTask;
    private bool _isRunning;
    private const int MaxConcurrentJobs = 5;

    public QueueWorker(IJobQueue jobQueue, IJobProcessor jobProcessor, IAgentLogger logger, IEventBus eventBus)
    {
        _jobQueue = jobQueue;
        _jobProcessor = jobProcessor;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _logger.LogInformation(LogCategory.Application, "Queue Worker started");

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _workerCts.Token);

        _workerTask = Task.Run(() => ExecuteWorkerLoopAsync(linkedCts.Token), linkedCts.Token);
    }

    public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _workerCts.Cancel();

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

        _logger.LogInformation(LogCategory.Application, "Queue Worker stopped");
    }

    private async Task ExecuteWorkerLoopAsync(CancellationToken cancellationToken)
    {
        var activeTasks = new List<Task>();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _jobQueue.DequeueAsync(cancellationToken);

                    if (job != null)
                    {
                        if (activeTasks.Count >= MaxConcurrentJobs)
                        {
                            // Wait for at least one task to complete
                            var completedTask = await Task.WhenAny(activeTasks);
                            activeTasks.Remove(completedTask);
                        }

                        var jobTask = ProcessJobAsync(job, cancellationToken);
                        activeTasks.Add(jobTask);
                    }
                    else
                    {
                        // No jobs available, wait a bit
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(LogCategory.Exception, "Queue worker loop error", ex);
                    await Task.Delay(5000, cancellationToken);
                }
            }

            // Wait for remaining tasks to complete
            if (activeTasks.Any())
            {
                await Task.WhenAll(activeTasks);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Queue worker fatal error", ex);
        }
    }

    private async Task ProcessJobAsync(IJob job, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(LogCategory.Application, "Processing job {JobId} of type {JobType}", job.JobId, job.JobType);

            if (await _jobProcessor.CanProcessAsync(job, cancellationToken))
            {
                await _jobProcessor.ProcessJobAsync(job, cancellationToken);
                await _jobQueue.UpdateJobStateAsync(job.JobId, JobState.Completed, null, cancellationToken);
            }
            else
            {
                _logger.LogWarning(LogCategory.Application, "Job {JobId} cannot be processed by current processor", job.JobId);
                await _jobQueue.UpdateJobStateAsync(job.JobId, JobState.Failed, "No suitable processor", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Job {JobId} processing failed", ex, job.JobId);

            if (job.RetryCount < 3)
            {
                await _jobQueue.UpdateJobStateAsync(job.JobId, JobState.Failed, ex.Message, cancellationToken);
                await _jobQueue.RetryJobAsync(job.JobId, cancellationToken);
            }
            else
            {
                await _jobQueue.UpdateJobStateAsync(job.JobId, JobState.Failed, ex.Message, cancellationToken);
                await _jobQueue.MoveToDeadLetterAsync(job.JobId, cancellationToken);
            }
        }
    }
}
