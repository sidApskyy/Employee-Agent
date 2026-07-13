using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Core.Contracts.ApiContracts;
using RDCS.EmployeeAgent.Infrastructure.Api;
using RDCS.EmployeeAgent.Shared.Constants;

namespace RDCS.EmployeeAgent.Services.DeviceRegistration;

public class DeviceRegistrationService : BaseApiService, IDeviceRegistrationService
{
    private readonly IDeviceInfoProvider _deviceInfoProvider;

    public DeviceRegistrationService(
        IApiClient apiClient,
        IAgentLogger logger,
        IDeviceInfoProvider deviceInfoProvider)
        : base(apiClient, logger)
    {
        _deviceInfoProvider = deviceInfoProvider;
    }

    public async Task<string> RegisterDeviceAsync(DeviceInfo deviceInfo, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.DeviceRegistration, "Registering device {DeviceName}", deviceInfo.DeviceName);

        // Development mode: bypass API for testing
        #if DEBUG
        _logger.LogWarning(Core.Enums.LogCategory.DeviceRegistration, "Development mode: bypassing API device registration");
        var devDeviceId = "DEV_DEVICE_" + Guid.NewGuid().ToString().Substring(0, 8);
        _logger.LogInformation(Core.Enums.LogCategory.DeviceRegistration, "Device registered successfully with ID {DeviceId}", devDeviceId);
        return devDeviceId;
        #else
        var request = new RegisterDeviceRequest
        {
            EmployeeId = deviceInfo.EmployeeId,
            CompanyId = deviceInfo.CompanyId,
            DeviceName = deviceInfo.DeviceName,
            ComputerName = deviceInfo.ComputerName,
            MachineGuid = deviceInfo.MachineGuid,
            Fingerprint = deviceInfo.Fingerprint,
            OsVersion = deviceInfo.OsVersion,
            WindowsUsername = deviceInfo.WindowsUsername,
            Processor = deviceInfo.Processor,
            RamGb = deviceInfo.RamGb,
            DiskSizeGb = deviceInfo.DiskSizeGb,
            MacAddress = deviceInfo.MacAddress,
            AgentVersion = deviceInfo.AgentVersion
        };

        var response = await _apiClient.PostAsync<RegisterDeviceRequest, RegisterDeviceResponse>(
            ApiRoutes.RegisterDevice,
            request,
            cancellationToken);

        _logger.LogInformation(Core.Enums.LogCategory.DeviceRegistration, "Device registered successfully with ID {DeviceId}", response.DeviceId);

        return response.DeviceId;
        #endif
    }

    public async Task<DeviceInfo> CollectDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(Core.Enums.LogCategory.DeviceRegistration, "Collecting device information");
        return await _deviceInfoProvider.GetDeviceInfoAsync(cancellationToken);
    }
}
