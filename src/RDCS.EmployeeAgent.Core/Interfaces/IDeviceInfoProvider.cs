using RDCS.EmployeeAgent.Core.Models;

namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IDeviceInfoProvider
{
    Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default);
}
