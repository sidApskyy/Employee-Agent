using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Core.Contracts.ApiContracts;
using RDCS.EmployeeAgent.Infrastructure.Api;
using RDCS.EmployeeAgent.Shared.Constants;

namespace RDCS.EmployeeAgent.Services.Heartbeat;

public class HeartbeatService : BaseApiService, IHeartbeatService
{
    public HeartbeatService(
        IApiClient apiClient,
        IAgentLogger logger)
        : base(apiClient, logger)
    {
    }

    public async Task SendHeartbeatAsync(HeartbeatPayload payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(Core.Enums.LogCategory.Heartbeat, "Sending heartbeat for device {DeviceId}", payload.DeviceId);

        var request = new HeartbeatRequest
        {
            EmployeeId = payload.EmployeeId,
            DeviceId = payload.DeviceId,
            AgentVersion = payload.AgentVersion,
            ComputerName = payload.ComputerName,
            IsOnline = payload.IsOnline,
            Timestamp = payload.Timestamp,
            ConfigVersion = payload.ConfigVersion,
            SystemMetrics = payload.SystemMetrics != null ? new SystemMetricsDto
            {
                CpuPercent = payload.SystemMetrics.CpuPercent,
                MemoryUsedMb = payload.SystemMetrics.MemoryUsedMb
            } : null
        };

        var response = await _apiClient.PostAsync<HeartbeatRequest, HeartbeatResponse>(
            ApiRoutes.Heartbeat,
            request,
            cancellationToken);

        _logger.LogDebug(Core.Enums.LogCategory.Heartbeat, "Heartbeat acknowledged. Next interval: {Interval}s", response.NextHeartbeatIntervalSeconds);

        if (response.RequiresLogout)
        {
            _logger.LogWarning(Core.Enums.LogCategory.Heartbeat, "Server requested logout");
        }

        if (response.IsBlocked)
        {
            _logger.LogError(Core.Enums.LogCategory.Heartbeat, "Device is blocked by server");
        }
    }
}
