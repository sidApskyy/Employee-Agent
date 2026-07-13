using Microsoft.Extensions.Hosting;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.Health;

namespace RDCS.EmployeeAgent.Runtime.HostedServices;

public class HealthMonitorBackgroundService : BackgroundService
{
    private readonly IHealthMonitor _healthMonitor;
    private readonly IAgentLogger _logger;

    public HealthMonitorBackgroundService(IHealthMonitor healthMonitor, IAgentLogger logger)
    {
        _healthMonitor = healthMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogCategory.Application, "Health Monitor Background Service started");

        await _healthMonitor.StartMonitoringAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var health = await _healthMonitor.GetOverallHealthAsync(stoppingToken);

                if (health == HealthStatus.Unhealthy)
                {
                    _logger.LogWarning(LogCategory.Application, "System health is unhealthy");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Exception, "Health Monitor Background Service error", ex);
            }
        }

        await _healthMonitor.StopMonitoringAsync(stoppingToken);

        _logger.LogInformation(LogCategory.Application, "Health Monitor Background Service stopped");
    }
}
