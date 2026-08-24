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
        // First attempt: fast path (backend is usually warm)
        if (await TryCheckAsync(TimeSpan.FromSeconds(8), cancellationToken))
        {
            SetOnline(true);
            return true;
        }

        // Second attempt: longer timeout to absorb Render free-tier cold starts
        // (cold start can take 20-50s). Avoids false "offline" that stalls uploads.
        ScreenshotWorkerTracer.Trace("CONNECTIVITY: Fast check failed, retrying with extended timeout (cold-start tolerance)");
        if (await TryCheckAsync(TimeSpan.FromSeconds(35), cancellationToken))
        {
            SetOnline(true);
            return true;
        }

        SetOnline(false);
        return false;
    }

    private async Task<bool> TryCheckAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            // Use HTTP instead of ICMP ping — many firewalls block ping but allow HTTPS
            using var client = new HttpClient { Timeout = timeout };
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://employee-agent-k1n2.onrender.com/api/agent/health");
            var response = await client.SendAsync(request, cancellationToken);
            var online = (int)response.StatusCode < 500;
            ScreenshotWorkerTracer.Trace($"CONNECTIVITY: HTTP check result={online}, StatusCode={response.StatusCode}, Timeout={timeout.TotalSeconds}s");
            return online;
        }
        catch (Exception ex)
        {
            ScreenshotWorkerTracer.Trace($"CONNECTIVITY: Check FAILED {ex.GetType().Name}: {ex.Message}, Timeout={timeout.TotalSeconds}s");
            return false;
        }
    }

    private void SetOnline(bool online)
    {
        if (online != _isOnline)
        {
            _isOnline = online;
            ConnectivityChanged?.Invoke(this, _isOnline);
        }
    }
}
