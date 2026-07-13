using Microsoft.Extensions.Hosting;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;

namespace RDCS.EmployeeAgent.Runtime.HostedServices;

public class EventQueueBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IAgentLogger _logger;

    public EventQueueBackgroundService(IEventBus eventBus, IAgentLogger logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogCategory.Application, "Event Queue Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process events - the EventBus handles this internally
                // This service keeps the EventBus alive and monitored
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Exception, "Event Queue Background Service error", ex);
            }
        }

        _logger.LogInformation(LogCategory.Application, "Event Queue Background Service stopped");
    }
}
