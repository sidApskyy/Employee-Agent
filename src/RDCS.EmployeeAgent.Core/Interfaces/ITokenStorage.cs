using RDCS.EmployeeAgent.Core.Models;

namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface ITokenStorage
{
    Task StoreTokensAsync(AgentIdentity identity, CancellationToken cancellationToken = default);
    Task<AgentIdentity?> RetrieveTokensAsync(CancellationToken cancellationToken = default);
    Task ClearTokensAsync(CancellationToken cancellationToken = default);
}
