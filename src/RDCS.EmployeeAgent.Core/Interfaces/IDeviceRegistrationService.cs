using RDCS.EmployeeAgent.Core.Models;

namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IDeviceRegistrationService
{
    Task<string> RegisterDeviceAsync(DeviceInfo deviceInfo, CancellationToken cancellationToken = default);
    Task<DeviceInfo> CollectDeviceInfoAsync(CancellationToken cancellationToken = default);
}
