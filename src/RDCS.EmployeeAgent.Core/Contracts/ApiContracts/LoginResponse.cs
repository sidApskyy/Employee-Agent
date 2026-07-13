namespace RDCS.EmployeeAgent.Core.Contracts.ApiContracts;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string EmployeeId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string ConfigVersion { get; set; } = string.Empty;
    public bool RequiresDeviceRegistration { get; set; }
}
