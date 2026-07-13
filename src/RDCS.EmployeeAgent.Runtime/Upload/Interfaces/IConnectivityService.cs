namespace RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

public interface IConnectivityService
{
    bool IsOnline { get; }
    bool IsMeteredConnection { get; }
    Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default);
    event EventHandler<bool> ConnectivityChanged;
}
