using Microsoft.Extensions.Hosting;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.Scheduler;

namespace RDCS.EmployeeAgent.Runtime.HostedServices;

public class SchedulerBackgroundService : BackgroundService
{
    private readonly IScheduler _scheduler;
    private readonly IAgentLogger _logger;

    public SchedulerBackgroundService(IScheduler scheduler, IAgentLogger logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogCategory.Application, "Scheduler Background Service started");

        await _scheduler.StartAsync(stoppingToken);

        // Keep running until cancellation
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _scheduler.StopAsync(stoppingToken);

        _logger.LogInformation(LogCategory.Application, "Scheduler Background Service stopped");
    }
}
