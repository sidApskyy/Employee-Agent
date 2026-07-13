using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using System.Net.NetworkInformation;

namespace RDCS.EmployeeAgent.Runtime.Upload.Services;

public class ConnectivityService : IConnectivityService
{
    private bool _isOnline = true;

    public bool IsOnline => _isOnline;
    public bool IsMeteredConnection => false;

    public event EventHandler<bool>? ConnectivityChanged;

    public async Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 3000);
            var online = reply.Status == IPStatus.Success;

            if (online != _isOnline)
            {
                _isOnline = online;
                ConnectivityChanged?.Invoke(this, _isOnline);
            }

            return _isOnline;
        }
        catch
        {
            if (_isOnline)
            {
                _isOnline = false;
                ConnectivityChanged?.Invoke(this, false);
            }
            return false;
        }
    }
}
