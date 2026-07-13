using RDCS.EmployeeAgent.Core.Models;

namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IHeartbeatService
{
    Task SendHeartbeatAsync(HeartbeatPayload payload, CancellationToken cancellationToken = default);
}
