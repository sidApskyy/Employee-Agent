using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using RDCS.EmployeeAgent.Runtime.Screenshot.Diagnostics;
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
            // Use HTTP instead of ICMP ping — many firewalls block ping but allow HTTPS
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://employee-agent-k1n2.onrender.com/api/agent/health");
            var response = await client.SendAsync(request, cancellationToken);
            var online = (int)response.StatusCode < 500;

            if (online != _isOnline)
            {
                _isOnline = online;
                ConnectivityChanged?.Invoke(this, _isOnline);
            }

            ScreenshotWorkerTracer.Trace($"CONNECTIVITY: HTTP check result={online}, StatusCode={response.StatusCode}");
            return _isOnline;
        }
        catch (Exception ex)
        {
            ScreenshotWorkerTracer.Trace($"CONNECTIVITY: Check FAILED {ex.GetType().Name}: {ex.Message}");
            if (_isOnline)
            {
                _isOnline = false;
                ConnectivityChanged?.Invoke(this, false);
            }
            return false;
        }
    }
}
