namespace RDCS.EmployeeAgent.Core.Contracts.ApiContracts;

public class HeartbeatResponse
{
    public int NextHeartbeatIntervalSeconds { get; set; } = 60;
    public string ConfigVersion { get; set; } = string.Empty;
    public bool ConfigChanged { get; set; }
    public bool IsBlocked { get; set; }
    public bool RequiresLogout { get; set; }
}
