namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface INetworkMonitorService
{
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    Task StopMonitoringAsync();
    bool IsMonitoring { get; }
    event EventHandler NetworkLost;
    event EventHandler NetworkRestored;
}
