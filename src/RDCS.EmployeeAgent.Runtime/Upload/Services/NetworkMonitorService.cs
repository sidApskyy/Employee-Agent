using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

namespace RDCS.EmployeeAgent.Runtime.Upload.Services;

public class NetworkMonitorService : INetworkMonitorService
{
    private readonly IConnectivityService _connectivityService;
    private readonly IAgentLogger _logger;
    private CancellationTokenSource? _monitorCts;
    private bool _wasOnline = true;

    public bool IsMonitoring { get; private set; }
    public event EventHandler? NetworkLost;
    public event EventHandler? NetworkRestored;

    public NetworkMonitorService(IConnectivityService connectivityService, IAgentLogger logger)
    {
        _connectivityService = connectivityService;
        _logger = logger;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (IsMonitoring) return;

        _monitorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsMonitoring = true;
        _logger.LogInformation(LogCategory.Application, "NetworkMonitorService started");

        _ = Task.Run(() => MonitorLoopAsync(_monitorCts.Token), _monitorCts.Token);
        await Task.CompletedTask;
    }

    public async Task StopMonitoringAsync()
    {
        _monitorCts?.Cancel();
        IsMonitoring = false;
        _logger.LogInformation(LogCategory.Application, "NetworkMonitorService stopped");
        await Task.CompletedTask;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var isOnline = await _connectivityService.CheckConnectivityAsync(cancellationToken);

                if (!isOnline && _wasOnline)
                {
                    _wasOnline = false;
                    _logger.LogWarning(LogCategory.Application, "NetworkMonitorService: Network LOST");
                    NetworkLost?.Invoke(this, EventArgs.Empty);
                }
                else if (isOnline && !_wasOnline)
                {
                    _wasOnline = true;
                    _logger.LogInformation(LogCategory.Application, "NetworkMonitorService: Network RESTORED");
                    NetworkRestored?.Invoke(this, EventArgs.Empty);
                }

                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Exception, "NetworkMonitorService loop error", ex);
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }
}
