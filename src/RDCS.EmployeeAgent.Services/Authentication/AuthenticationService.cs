using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Core.Contracts.ApiContracts;
using RDCS.EmployeeAgent.Infrastructure.Api;
using RDCS.EmployeeAgent.Shared.Constants;

namespace RDCS.EmployeeAgent.Services.Authentication;

public class AuthenticationService : BaseApiService, IAuthenticationService
{
    private readonly ITokenStorage _tokenStorage;

    public AuthenticationService(
        IApiClient apiClient,
        IAgentLogger logger,
        ITokenStorage tokenStorage)
        : base(apiClient, logger)
    {
        _tokenStorage = tokenStorage;
    }

    public async Task<AgentIdentity> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Authentication, "Attempting login for user {Email}", email);

        // Development mode: bypass API for testing
        #if DEBUG
        _logger.LogWarning(Core.Enums.LogCategory.Authentication, "Development mode: bypassing API authentication");
        
        var identity = new AgentIdentity
        {
            AccessToken = "dev_access_token_" + Guid.NewGuid(),
            RefreshToken = "dev_refresh_token_" + Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            EmployeeId = "DEV001",
            CompanyId = "COMP001",
            DeviceId = "DEVICE001",
            ConfigVersion = "1.0.0",
            RequiresDeviceRegistration = false
        };

        await _tokenStorage.StoreTokensAsync(identity, cancellationToken);
        _logger.LogInformation(Core.Enums.LogCategory.Authentication, "Development login successful for user {Email}", email);
        return identity;
        #else
        var request = new LoginRequest
        {
            Email = email,
            Password = password,
            ClientVersion = "1.0.0",
            Environment = "production"
        };

        var response = await _apiClient.PostAsync<LoginRequest, LoginResponse>(
            ApiRoutes.Login,
            request,
            cancellationToken);

        var identity = new AgentIdentity
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn),
            EmployeeId = response.EmployeeId,
            CompanyId = response.CompanyId,
            DeviceId = response.DeviceId ?? string.Empty,
            ConfigVersion = response.ConfigVersion,
            RequiresDeviceRegistration = response.RequiresDeviceRegistration
        };

        await _tokenStorage.StoreTokensAsync(identity, cancellationToken);

        _logger.LogInformation(Core.Enums.LogCategory.Authentication, "Login successful for user {Email}", email);

        return identity;
        #endif
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Authentication, "Logging out user");
        await _tokenStorage.ClearTokensAsync(cancellationToken);
    }

    public async Task<AgentIdentity?> GetStoredIdentityAsync(CancellationToken cancellationToken = default)
    {
        return await _tokenStorage.RetrieveTokensAsync(cancellationToken);
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var identity = await _tokenStorage.RetrieveTokensAsync(cancellationToken);
        return identity != null && !string.IsNullOrEmpty(identity.AccessToken) && identity.ExpiresAt > DateTime.UtcNow;
    }

    public async Task<AgentIdentity> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Authentication, "Refreshing access token");

        var identity = await _tokenStorage.RetrieveTokensAsync(cancellationToken);
        if (identity == null || string.IsNullOrEmpty(identity.RefreshToken))
        {
            throw new Core.Exceptions.AgentException("No stored identity found for token refresh");
        }

        #if DEBUG
        identity.ExpiresAt = DateTime.UtcNow.AddHours(24);
        await _tokenStorage.StoreTokensAsync(identity, cancellationToken);
        return identity;
        #else
        var response = await _apiClient.PostAsync<object, RefreshResponse>(
            ApiRoutes.RefreshToken,
            new { refreshToken = identity.RefreshToken },
            cancellationToken);

        identity.AccessToken = response.AccessToken;
        identity.RefreshToken = response.RefreshToken;
        identity.ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn);

        await _tokenStorage.StoreTokensAsync(identity, cancellationToken);

        _logger.LogInformation(Core.Enums.LogCategory.Authentication, "Access token refreshed successfully");

        return identity;
        #endif
    }
}
