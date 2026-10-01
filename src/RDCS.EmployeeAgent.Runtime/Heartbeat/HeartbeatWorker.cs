using System.Reflection;
using Microsoft.Extensions.Configuration;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Workers;

namespace RDCS.EmployeeAgent.Runtime.Heartbeat;

/// <summary>
/// Periodically reports agent liveness to the backend (employee_devices.last_seen_at / is_online).
/// Without this worker SendHeartbeatAsync is dead code and the admin can never tell whether
/// an agent is running, stopped, or stuck at a login screen after a reboot.
/// </summary>
public class HeartbeatWorker : BackgroundWorkerBase
{
    private readonly IHeartbeatService _heartbeatService;
    private readonly ITokenStorage _tokenStorage;
    private readonly IConfiguration _configuration;

    public override string Name => "HeartbeatWorker";

    public HeartbeatWorker(
        IHeartbeatService heartbeatService,
        ITokenStorage tokenStorage,
        IConfiguration configuration,
        IEventBus eventBus,
        IAgentLogger logger)
        : base(logger, eventBus)
    {
        _heartbeatService = heartbeatService;
        _tokenStorage = tokenStorage;
        _configuration = configuration;
        Configuration.ExecutionInterval = TimeSpan.FromSeconds(60);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        AgentIdentity? identity = null;
        try
        {
            identity = await _tokenStorage.RetrieveTokensAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(LogCategory.Application, "HeartbeatWorker: could not read stored identity - {Message}", ex.Message);
            return;
        }

        var employeeId = !string.IsNullOrEmpty(identity?.EmployeeId)
            ? identity!.EmployeeId
            : _configuration["Agent:EmployeeId"];
        var deviceId = !string.IsNullOrEmpty(identity?.DeviceId)
            ? identity!.DeviceId
            : _configuration["Agent:DeviceId"];

        // Not logged in yet — nothing meaningful to report against.
        if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(deviceId))
        {
            return;
        }

        var payload = new HeartbeatPayload
        {
            EmployeeId = employeeId,
            DeviceId = deviceId,
            AgentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            ComputerName = Environment.MachineName,
            IsOnline = true,
            Timestamp = DateTime.UtcNow,
            ConfigVersion = identity?.ConfigVersion ?? _configuration["Agent:ConfigVersion"] ?? "1.0.0",
        };

        await _heartbeatService.SendHeartbeatAsync(payload, cancellationToken);
    }

    protected override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        // Heartbeat failures are non-fatal (offline, expired token pending refresh,
        // backend restart). Reset State to Running — otherwise BackgroundWorkerBase
        // breaks the loop permanently and heartbeats stop until the next app restart.
        Logger.LogWarning(LogCategory.Application, "HeartbeatWorker: heartbeat failed - {Message}", exception.Message);
        State = WorkerState.Running;
        UpdateHealth(HealthStatus.Degraded, $"Recovered from error: {exception.Message}");
        return Task.CompletedTask;
    }
}
