using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Services.Orchestration;

public class ApplicationOrchestrator
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IDeviceRegistrationService _deviceRegistrationService;
    private readonly IConfigurationService _configurationService;
    private readonly IModuleHost _moduleHost;
    private readonly IAgentLogger _logger;

    public ApplicationOrchestrator(
        IAuthenticationService authenticationService,
        IDeviceRegistrationService deviceRegistrationService,
        IConfigurationService configurationService,
        IModuleHost moduleHost,
        IAgentLogger logger)
    {
        _authenticationService = authenticationService;
        _deviceRegistrationService = deviceRegistrationService;
        _configurationService = configurationService;
        _moduleHost = moduleHost;
        _logger = logger;
    }

    public async Task<AgentStatus> InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Application, "Initializing application");

        var isAuthenticated = await _authenticationService.IsAuthenticatedAsync(cancellationToken);

        if (!isAuthenticated)
        {
            _logger.LogInformation(Core.Enums.LogCategory.Application, "User not authenticated");
            return AgentStatus.Authenticating;
        }

        var identity = await _authenticationService.GetStoredIdentityAsync(cancellationToken);
        if (identity == null)
        {
            _logger.LogWarning(Core.Enums.LogCategory.Application, "No stored identity found");
            return AgentStatus.Authenticating;
        }

        if (identity.RequiresDeviceRegistration || string.IsNullOrEmpty(identity.DeviceId))
        {
            _logger.LogInformation(Core.Enums.LogCategory.Application, "Device registration required");
            return AgentStatus.RegisteringDevice;
        }

        _logger.LogInformation(Core.Enums.LogCategory.Application, "User authenticated, starting modules");
        await _moduleHost.StartAllModulesAsync(cancellationToken);

        return AgentStatus.Running;
    }

    public async Task<bool> LoginAndRegisterDeviceAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(Core.Enums.LogCategory.Application, "Starting login and device registration flow");

            var identity = await _authenticationService.LoginAsync(email, password, cancellationToken);

            if (identity.RequiresDeviceRegistration || string.IsNullOrEmpty(identity.DeviceId))
            {
                _logger.LogInformation(Core.Enums.LogCategory.Application, "Device registration required after login");

                var deviceInfo = await _deviceRegistrationService.CollectDeviceInfoAsync(cancellationToken);
                deviceInfo.EmployeeId = identity.EmployeeId;
                deviceInfo.CompanyId = identity.CompanyId;

                var deviceId = await _deviceRegistrationService.RegisterDeviceAsync(deviceInfo, cancellationToken);
                identity.DeviceId = deviceId;
                identity.RequiresDeviceRegistration = false;

                await _authenticationService.LogoutAsync(cancellationToken);
                await _authenticationService.LoginAsync(email, password, cancellationToken);
            }

            await DownloadAndApplyConfigurationAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(Core.Enums.LogCategory.Application, "Login and device registration failed", ex);
            return false;
        }
    }

    public async Task DownloadAndApplyConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Application, "Downloading and applying configuration");

        var identity = await _authenticationService.GetStoredIdentityAsync(cancellationToken);
        if (identity == null)
        {
            throw new Core.Exceptions.AgentException("No identity found for configuration download");
        }

        var configuration = await _configurationService.DownloadConfigurationAsync(identity.ConfigVersion, cancellationToken);
        await _configurationService.ApplyConfigurationAsync(configuration, cancellationToken);

        _logger.LogInformation(Core.Enums.LogCategory.Application, "Configuration applied successfully");
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Application, "Shutting down application");
        await _moduleHost.StopAllModulesAsync(cancellationToken);
        await _authenticationService.LogoutAsync(cancellationToken);
    }
}
