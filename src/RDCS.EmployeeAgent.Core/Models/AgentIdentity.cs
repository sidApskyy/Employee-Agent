namespace RDCS.EmployeeAgent.Core.Models;

public class AgentIdentity
{
    public string EmployeeId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string ConfigVersion { get; set; } = string.Empty;
    public bool RequiresDeviceRegistration { get; set; }
}
