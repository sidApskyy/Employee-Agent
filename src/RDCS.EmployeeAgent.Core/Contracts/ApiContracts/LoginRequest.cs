namespace RDCS.EmployeeAgent.Core.Contracts.ApiContracts;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "production";
}
