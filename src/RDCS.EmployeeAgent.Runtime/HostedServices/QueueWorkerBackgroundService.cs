using Microsoft.Extensions.Hosting;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.Queue;

namespace RDCS.EmployeeAgent.Runtime.HostedServices;

public class QueueWorkerBackgroundService : BackgroundService
{
    private readonly IQueueWorker _queueWorker;
    private readonly IAgentLogger _logger;

    public QueueWorkerBackgroundService(IQueueWorker queueWorker, IAgentLogger logger)
    {
        _queueWorker = queueWorker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogCategory.Application, "Queue Worker Background Service started");

        await _queueWorker.StartProcessingAsync(stoppingToken);

        // Keep running until cancellation
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _queueWorker.StopProcessingAsync(stoppingToken);

        _logger.LogInformation(LogCategory.Application, "Queue Worker Background Service stopped");
    }
}
