namespace RDCS.EmployeeAgent.Core.Contracts.ApiContracts;

public class RegisterDeviceResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string ConfigVersion { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public DateTime RegisteredAt { get; set; }
}
