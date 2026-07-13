using RDCS.EmployeeAgent.Core.Models;

namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IAuthenticationService
{
    Task<AgentIdentity> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<AgentIdentity?> GetStoredIdentityAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
    Task<AgentIdentity> RefreshTokenAsync(CancellationToken cancellationToken = default);
}
